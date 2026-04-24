#nullable enable
using GeminiLab.Core.Events;
using GeminiLab.Modules.Gateway;
using GeminiLab.Modules.UI.ViewModels;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class ChatViewModelTests
    {
        [Test]
        public void ReceivesChunkAndDoneEvents_UpdatesViewState()
        {
            EventBus eventBus = new();
            using ChatViewModel viewModel = new(eventBus);

            eventBus.Publish(new GatewayChatChunkEvent("trace_ui", "hello"));
            eventBus.Publish(new GatewayChatChunkEvent("trace_ui", " world"));
            eventBus.Publish(new GatewayChatDoneEvent("trace_ui", "done text"));

            Assert.AreEqual("trace_ui", viewModel.CurrentTraceId);
            Assert.AreEqual("hello world", viewModel.StreamingText);
            Assert.AreEqual("done text", viewModel.LastCompletedSummary);
        }
    }
}
