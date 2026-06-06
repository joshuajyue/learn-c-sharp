// Lesson 16: Speech, and a pointer to realtime audio
// ============================================================================
//
// FROM TEXT-IN/TEXT-OUT TO VOICE
// ----------------------------------------------------------------------------
// Every lesson up to now has been keyboard-in, screen-out. Real assistants
// (Siri, Alexa, ChatGPT Voice, in-car infotainment) take VOICE input and
// respond with VOICE output. M.E.AI handles voice in two distinct styles,
// each with its own abstraction:
//
//   1. DISCRETE / "PUSH-TO-TALK"
//        User holds a button, speaks, releases. The recorded audio is
//        transcribed in one shot; the assistant produces one reply; the
//        reply is synthesised to audio and played. This is the lesson.
//
//   2. REALTIME / "ALWAYS-ON"
//        A bidirectional audio stream stays open while the user speaks. The
//        model produces partial transcripts AND audio replies as it goes.
//        OpenAI's Realtime API and gpt-4o-realtime are the canonical
//        examples. Conceptually mentioned at the end of this lesson; the
//        full API is out of scope for "basics".
//
//
// THE DISCRETE-VOICE ABSTRACTIONS
// ----------------------------------------------------------------------------
// They MIRROR `IChatClient`'s shape so you can compose them with the same
// builder pipeline:
//
//   ISpeechToTextClient                      audio bytes -> transcript
//       GetTextAsync(audioStream, ...)              -- full transcript
//       GetStreamingTextAsync(audioStream, ...)     -- partial transcripts
//
//   (parallel) ITextToSpeechClient            transcript -> audio bytes
//       Lives alongside SpeechToText/ in the abstractions package.
//
// Source: dotnet/extensions
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/SpeechToText/
//
// NOTE: `ISpeechToTextClient` is currently marked `[Experimental("MEAI001")]`
// in the package. The diagnostic warns that the API may change. We suppress
// it in this lesson's .csproj (`<NoWarn>$(NoWarn);MEAI001</NoWarn>`) because
// it's the right primitive to teach; you'll see the same warning if you
// reach for it directly in your own code.
//
//
// A TYPICAL VOICE-ASSISTANT PIPELINE
// ----------------------------------------------------------------------------
//   mic capture --> wav bytes --> ISpeechToTextClient   --> transcript
//                              --> IChatClient (lessons 1-13) --> reply text
//                              --> ITextToSpeechClient   --> wav bytes
//                              --> speaker
//
// Every box except the mic and speaker is something M.E.AI provides an
// abstraction for. The middleware machinery from lesson 08 composes around
// each one independently: cache the STT result, log the chat call, etc.
//
//
// WHAT THIS LESSON ACTUALLY DOES
// ----------------------------------------------------------------------------
//   * defines a tiny `ISpeechToTextClient` fake that "transcribes" by
//     reading the first byte of the audio stream and picking a canned phrase,
//   * pipes the transcript through your existing `IChatClient`,
//   * "speaks" the reply by writing it to the console (we don't actually
//     synthesise audio -- that requires platform-specific bindings),
//   * shows the STREAMING variant of STT (live captions), and
//   * sketches what a realtime audio session looks like, with a pointer to
//     the abstractions namespace for further reading.
//
//
// WHY FAKES?
// ----------------------------------------------------------------------------
// Real STT models (whisper, deepgram, etc.) need a model server. Real TTS
// models need an audio device. Both are environmental dependencies the
// lesson avoids. The CONSUMER code at the top of this file -- the three
// `await` calls and the `await foreach` -- is identical against a real
// implementation; only the construction of `FakeSpeechToText` changes.

using Microsoft.Extensions.AI;

// Wire up the discrete voice loop. As always, declare against the
// INTERFACE so swapping in a real provider is a one-liner.
ISpeechToTextClient stt = new FakeSpeechToText();
IChatClient        chat = new FakeAssistant();

// Pretend the user just held the mic and said "What's the weather like?"
// In a real app these bytes would come from NAudio (Windows), AVFoundation
// (macOS/iOS), Web Audio (browser via Blazor), or a phone app's recorder.
//
// `MemoryStream` is the in-memory `Stream` implementation -- the abstract
// class real STT clients consume. Wrapping it in `using var ...` (the
// using-declaration from lesson 09) means it gets Disposed when the
// enclosing scope ends.
using var fakeAudio = new MemoryStream([0x01, 0x02, 0x03]);

// 1. TRANSCRIBE. `GetTextAsync` returns a `SpeechToTextResponse` whose
//    `.Text` is the full transcript. Real implementations also expose
//    word-level timestamps and confidence scores, but the abstraction
//    keeps the basic case dead simple.
var transcriptResponse = await stt.GetTextAsync(fakeAudio);
var transcript = transcriptResponse.Text;
Console.WriteLine($"user (heard) : {transcript}");

// 2. CHAT. The transcript becomes a USER message to a plain IChatClient.
//    Crucially, all the middleware tricks from earlier lessons compose
//    around this step -- function calling, structured output, caching --
//    you don't need anything voice-specific for the LLM half.
var reply = await chat.GetResponseAsync(
    [
        new(ChatRole.System, "You are a brief voice assistant. Keep answers under 20 words."),
        new(ChatRole.User,   transcript),
    ]);

// 3. "SPEAK" the reply. A real `ITextToSpeechClient` would return audio
//    bytes you'd feed to the platform's audio output. We just print --
//    sufficient for the lesson, hopeless for a real product.
Console.WriteLine($"assistant    : {reply.Text}");
Console.WriteLine($"[would synthesise to speech and play back through speakers]");


// --- Streaming demo: live captions ----------------------------------------
//
// `GetStreamingTextAsync` yields `SpeechToTextResponseUpdate` values as the
// STT model commits each word. That's the "voice typing" UX where the
// transcript appears WHILE the user is still talking. Same shape as
// `IChatClient.GetStreamingResponseAsync` from lesson 02.
//
// `await foreach` (lesson 02) walks an `IAsyncEnumerable<T>` element by
// element, awaiting each. The thread stays free between elements.
Console.WriteLine("\nlive captions demo:");
using var liveAudio = new MemoryStream([0x10, 0x20, 0x30]);
await foreach (var u in stt.GetStreamingTextAsync(liveAudio))
{
    Console.Write(u.Text);
}
Console.WriteLine();


// --- Realtime sketch ------------------------------------------------------
//
// A realtime session is conceptually:
//
//   var session = await realtimeClient.StartAsync(...);
//   await session.SendAudioAsync(micChunk);     // push more audio whenever
//   await foreach (var ev in session.Events)    // mixed audio/text/tool events
//   {
//       switch (ev) { ... render audio, react to function calls ... }
//   }
//
// The interesting differences vs. discrete STT+TTS:
//   * The model can BARGE IN (interrupt itself) when the user starts
//     speaking, and resume cleanly.
//   * Latency is dramatically lower (~300 ms total mouth-to-ear vs ~2-3
//     seconds for pipelined STT->LLM->TTS).
//   * Tool calls happen MID-CONVERSATION; the model can pause, fetch a fact,
//     and continue speaking in the same audio stream.
//
// See `src/Libraries/Microsoft.Extensions.AI.Abstractions/Realtime/` for
// the actual interfaces and the OpenAI Realtime sample for a full app.
Console.WriteLine("\n(realtime audio-to-audio is in M.E.AI.Abstractions/Realtime/ -- see README)");


// --- Fakes ----------------------------------------------------------------
//
// Tiny `ISpeechToTextClient` that "transcribes" by reading the FIRST BYTE
// of the audio stream to pick a canned phrase. Real implementations live in
// packages like `Microsoft.Extensions.AI.OpenAI` (whisper-1) or community
// packages for deepgram, Azure Cognitive Services, etc.
internal sealed class FakeSpeechToText : ISpeechToTextClient
{
    public Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // `Stream.ReadByte()` returns the next byte as int, or -1 on EOF.
        // Java analogue: `InputStream.read()` (same signature, same -1
        // convention).
        var first = audioSpeechStream.ReadByte();

        // SWITCH EXPRESSION (C# 8): produces a value, not a control flow.
        // The PATTERN syntax `case-arm => value;` is required. `_` is the
        // discard pattern (matches anything; Java's `default`). Vastly
        // more concise than the classic `switch` statement when you just
        // need value selection.
        string transcript = first switch
        {
            0x01 => "What's the weather like?",
            _    => "Hello there.",
        };
        return Task.FromResult(new SpeechToTextResponse(transcript));
    }

    // ASYNC ITERATOR (see lesson 02 for the longer explanation of
    // `async IAsyncEnumerable<T>` + `yield return`). The
    // `[EnumeratorCancellation]` attribute glues the consumer's
    // cancellation token to ours -- without it the token would be silently
    // ignored. The fully-qualified attribute name (`System.Runtime
    // .CompilerServices.EnumeratorCancellation`) avoids a `using` directive
    // for one attribute.
    public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        // Yield three canned "words" with delay so the typewriter effect
        // is visible. Real STT models emit partial transcripts as their
        // beam search firms up; the cadence is similar.
        foreach (var word in new[] { "Hello ", "there, ", "world." })
        {
            await Task.Delay(100, cancellationToken);
            yield return new SpeechToTextResponseUpdate(word);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}


// --- Toy chat client -- same shape as every other fake in this track ------
// Picks one of two replies based on whether the user mentioned "weather".
// In a real voice app, this slot would be a real LLM with the same
// middleware pipeline you've been building since lesson 08.
internal sealed class FakeAssistant : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var question = messages.Last(m => m.Role == ChatRole.User).Text!.ToLowerInvariant();
        var reply = question.Contains("weather")
            ? "It's currently fifty-three and drizzly."
            : "I'm not sure what to say to that.";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(IChatClient) && serviceKey is null ? this : null;

    public void Dispose() { }
}
