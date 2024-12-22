using Ginger.Properties;
using System;
using System.Windows.Forms;
using WinFormsSyntaxHighlighter;

namespace Ginger
{
	public abstract class TextParameterPanelBase : ParameterPanel<TextParameter>, ISyntaxHighlighted, ISearchableContainer
	{
		protected override CheckBox parameterCheckBox { get { return _cbEnabled; } }
		protected override Label parameterLabel { get { return _label; } }

		private Label _label;
		private FlatRichTextBox _textBox;
		private CheckBox _cbEnabled;
		protected int _contentHash;

		protected RichTextBoxEx richTextBox { get { return _textBox.richTextBox; } }

		public static bool AllowFlexibleHeight = true; // Controls flexible parameters

		public void Init(Label label, FlatRichTextBox textBox, CheckBox cbEnabled, Control editButton)
		{
			_label = label;
			_cbEnabled = cbEnabled;
			_textBox = textBox;
			_cbEnabled.CheckedChanged += CbEnabled_CheckedChanged;

			richTextBox.SetLineHeight(Constants.LineHeight);
			richTextBox.ValueChanged += OnValueChanged;
			richTextBox.GotFocus += RichTextBox_GotFocus;
			richTextBox.LostFocus += RichTextBox_LostFocus;
			richTextBox.ControlEnterPressed += TextBox_OnControlEnterPressed;
			richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.Default;

			if (editButton != null)
			{
				editButton.MouseClick += new MouseEventHandler(BtnWrite_MouseClick);
				SetTooltip(Resources.tooltip_open_write, editButton);
			}

			FontChanged += FontDidChange;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (_label != null)
				SizeLabel(_label);
			if (_textBox != null)
				SizeToWidth(_textBox);
		}

		private void RichTextBox_GotFocus(object sender, EventArgs e)
		{
			if (isIgnoringEvents || !Enabled)
				return;

			_contentHash = _textBox.Text.GetHashCode();
		}

		private void RichTextBox_LostFocus(object sender, EventArgs e)
		{
			if (isIgnoringEvents || !Enabled)
				return;

			var newContentHash = _textBox.Text.GetHashCode();
			if (_contentHash != newContentHash)
			{
				Current.IsFileDirty = this.parameter.value != _textBox.Text;
				this.parameter.value = _textBox.Text;

				_contentHash = newContentHash;
				NotifyValueChanged(_contentHash);
			}
		}

		protected void FontDidChange(object sender, EventArgs e)
		{
			WhileIgnoringEvents(() => {
				_textBox.Font = this.Font;
			});
		}

		protected override void OnSetParameter()
		{
			_textBox.Enabled = parameter.isEnabled || !parameter.isOptional;
			_textBox.Placeholder = parameter.placeholder;
			_textBox.InitUndo();
			_textBox.ApplyVisualTheme();

			// Tooltip
			SetTooltip(_label, _textBox);
		}

		protected override void OnRefreshValue()
		{
			_textBox.richTextBox.SetTextSilent(parameter.value);
			_textBox.InitUndo();
			_contentHash = _textBox.richTextBox.Text.GetHashCode();
			_cbEnabled.Checked = this.parameter.isEnabled;
		}

		protected override void OnSetEnabled(bool bEnabled)
		{
			_cbEnabled.Enabled = bEnabled && parameter.isOptional;
			_textBox.Enabled = bEnabled && parameter.isEnabled;
		}

		protected override void OnSetReserved(bool bReserved, string reservedValue)
		{ 
			_cbEnabled.Enabled = !bReserved && parameter.isOptional;
			_textBox.Enabled = !bReserved && parameter.isEnabled;
			
			WhileIgnoringEvents(() => {
				_textBox.Text = bReserved ? reservedValue : parameter.value;
				_textBox.InitUndo();
			});
			
		}

		private void CbEnabled_CheckedChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			_textBox.Enabled = _cbEnabled.Checked || !parameter.isOptional;
			if (isIgnoringEvents)
				return;
			parameter.isEnabled = _cbEnabled.Checked || !parameter.isOptional;
			NotifyEnabledChanged();
		}

		private void OnValueChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents || Enabled == false)
				return;

			this.parameter.value = _textBox.Text;
			Current.IsFileDirty = true;
		}

		private void BtnWrite_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && richTextBox.Enabled)
				ShowWriteDialog(richTextBox);
		}

		private void TextBox_OnControlEnterPressed(object sender, EventArgs e)
		{
			if (richTextBox.Multiline)
				ShowWriteDialog(richTextBox);
		}

		public ISearchable[] GetSearchables()
		{
			return new ISearchable[] { richTextBox };
		}

		protected bool ShowWriteDialog(RichTextBoxEx textBox)
		{
			MainForm.HideFindDialog();

			textBox.Focus();

			using (var dlg = new WriteDialog())
			{
				dlg.Text = Utility.EscapeMenu(parameter.label);
				dlg.Value = textBox.Text;
				dlg.SelectionStart = textBox.SelectionStart;
				dlg.SelectionLength = textBox.SelectionLength;

				var contentHash = textBox.Text.GetHashCode();
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					this.parameter.value = dlg.Value;

					WhileIgnoringEvents(() => {
						textBox.SetText(dlg.Value, dlg.SelectionStart, dlg.SelectionLength);
						textBox.ScrollToSelection();
					});

					var newContentHash = textBox.Text.GetHashCode();
					if (contentHash != newContentHash)
						NotifyValueChanged();

					return true;
				}
				return false;
			}
		}

		public void RefreshSyntaxHighlight(bool immediate, bool invalidate)
		{
			if (invalidate)
				richTextBox.InvalidateSyntaxHighlighting();
			richTextBox.RefreshSyntaxHighlight(immediate);
		}
	}
}
