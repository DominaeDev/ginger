using Ginger.Properties;
using Ginger.Integration;
using System.Windows.Forms;
using System.Text;
using System;

namespace Ginger
{
	public static class MsgBox
	{
		public static void Message(string message, string caption, IWin32Window owner = null)
		{
			MessageBox.Show(owner ?? MainForm.instance, message, caption, 
				MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		public static void Warning(string message, string caption, IWin32Window owner = null)
		{
			MessageBox.Show(owner ?? MainForm.instance, message, caption, 
				MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}

		public static void Error(string message, string caption, IWin32Window owner = null)
		{
			MessageBox.Show(owner ?? MainForm.instance, message, caption, 
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public static bool Ask(string message, string caption, IWin32Window owner = null)
		{
			return MessageBox.Show(owner ?? MainForm.instance, message, caption, 
				MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes;
		}

		public static bool AskOkCancel(string message, string caption, IWin32Window owner = null)
		{
			return MessageBox.Show(owner ?? MainForm.instance, message, caption, 
				MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1) == DialogResult.OK;
		}

		public static bool Confirm(string message, string caption, IWin32Window owner = null)
		{
			return MessageBox.Show(owner ?? MainForm.instance, message, caption, 
				MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
		}

		public static DialogResult AskYesNoCancel(string message, string caption, IWin32Window owner = null)
		{
			return MessageBox.Show(owner ?? MainForm.instance, message, caption, 
				MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
		}

		public static DialogResult ConfirmYesNoCancel(string message, string caption, IWin32Window owner = null)
		{
			return MessageBox.Show(owner ?? MainForm.instance, message, caption, 
				MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
		}

		public static class LinkError
		{
			public static void ConnectionFailed(string caption = null)
			{
				MsgBox.Error(WithReason(Resources.error_link_failed), caption ?? Resources.cap_link_error);
			}

			public static void Disconnected(string caption = null)
			{
				MsgBox.Error(WithReason(Resources.error_link_disconnected), caption ?? Resources.cap_link_error);
			}

			public static void RefreshFailed(string caption = null)
			{
				MsgBox.Error(WithReason(Resources.error_link_read_characters), caption ?? Resources.cap_link_error);
			}

			public static void Error(Backyard.Error error, string caption = null)
			{
				string errorMsg;

				switch (error)
				{
				case Backyard.Error.NoError:
					return;

				case Backyard.Error.NotConnected:
					errorMsg = Resources.error_link_disconnected;
					break;
				case Backyard.Error.InvalidArgument:
					errorMsg = "Invalid arguments.";
					break;
				case Backyard.Error.UnsupportedFeature:
					errorMsg = "Unsupported feature.";
					break;
				case Backyard.Error.NotFound:
					errorMsg = "Resource not found.";
					break;
				case Backyard.Error.DismissedByUser:
				case Backyard.Error.CancelledByUser:
					errorMsg = Resources.error_canceled;
					break;
				default:
					errorMsg = Resources.error_link_general;
					break;
				}

				MsgBox.Error(WithReason(errorMsg), caption ?? Resources.cap_link_error);
			}

			public static void Canceled(string caption = null)
			{
				MsgBox.Error(Resources.error_canceled, caption ?? Resources.cap_link_error);
			}

			private static string WithReason(string message)
			{
				string lastError = Backyard.LastError;
				if (string.IsNullOrEmpty(lastError))
					return message;
				Backyard.LastError = null;

				var sbError = new StringBuilder();
				sbError.Append(message);
				if (string.IsNullOrEmpty(lastError) == false)
				{
					sbError.NewParagraph();
					sbError.Append("Reason: ");
					sbError.Append(lastError);
				}

				return sbError.ToString();
			}
		}

	}
}
