using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace DisableScrollPastEnd {
	[Export(typeof(IMouseProcessorProvider))]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[Name("Disable Scroll Past End Processor")]
	internal class DisableScrollPastEndMouseProcessorProvider : IMouseProcessorProvider {
		IMouseProcessor IMouseProcessorProvider.GetAssociatedProcessor(IWpfTextView wpfTextView) {
			return new DisableScrollPastEndMouseProcessor(wpfTextView);
		}
	}

	internal class DisableScrollPastEndMouseProcessor : MouseProcessorBase {
		private readonly IWpfTextView _wpfTextView;

		internal DisableScrollPastEndMouseProcessor(IWpfTextView wpfTextView) {
			_wpfTextView = wpfTextView;
		}

		public override void PreprocessMouseWheel(MouseWheelEventArgs args) {
			try {
				ScrollDirection direction = ((args.Delta >= 0)
					? ScrollDirection.Up
					: ScrollDirection.Down
				);
				bool shouldScroll = false;
				ITextSnapshotLine lastLine = _wpfTextView.TextSnapshot.Lines.Last();
				IWpfTextViewLine lastVisibleLine = _wpfTextView.TextViewLines.LastVisibleLine;
				if (direction == ScrollDirection.Up) {
					shouldScroll = true;
				} else {
					bool lastLineVisible = lastVisibleLine.Extent == lastLine.Extent;
					if (!lastLineVisible) {
						shouldScroll = true;
					} else {
						if (lastVisibleLine.VisibilityState == VisibilityState.PartiallyVisible) {
							_wpfTextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, 1);
						}
					}
				}
				if (shouldScroll) {
					int lines = Math.Abs(args.Delta / 30);
					_wpfTextView.ViewScroller.ScrollViewportVerticallyByLines(direction, lines);
					IWpfTextViewLine lastVisibleLineAfter = _wpfTextView.TextViewLines.LastVisibleLine;
					bool lastLineVisibleAfter = lastVisibleLineAfter.Extent == lastLine.Extent;
					if (lastLineVisibleAfter) {
						double lineHeight = _wpfTextView.LineHeight;
						double linesTotal = _wpfTextView.TextBuffer.CurrentSnapshot.LineCount;
						double height = _wpfTextView.ViewportHeight;
						double visibleLines = Math.Floor(height / lineHeight);
						int lastLineShouldBeVisibleIndex = (int)(linesTotal - visibleLines); // I know this is 0-indexed, but I want to account for rounding, so I'm not going to subtract 1 here.
						if (lastLineShouldBeVisibleIndex < 0) {
							lastLineShouldBeVisibleIndex = 0;
						}
						ITextSnapshotLine shouldLastVisibleLine = null;
						try {
							shouldLastVisibleLine = _wpfTextView.TextSnapshot.GetLineFromLineNumber(lastLineShouldBeVisibleIndex);
						} catch {
							// Do nothing. It's possible that we provided it with something that is out of range.
						}
						if (shouldLastVisibleLine != null) {
							IWpfTextViewLine firstVisibleLineAfter = _wpfTextView.TextViewLines.FirstVisibleLine;
							while (firstVisibleLineAfter.Extent.Start > shouldLastVisibleLine.Extent.Start) {
								_wpfTextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Up, 1);
								firstVisibleLineAfter = _wpfTextView.TextViewLines.FirstVisibleLine;
							}
						}
					}
				}
				args.Handled = true;
			} catch {
				// Do nothing just in case something does go wrong, and then we can just silently not handle the event and the default scroller will take over.
			}
		}
	}
}
