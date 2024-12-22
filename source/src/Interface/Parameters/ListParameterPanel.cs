using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Ginger
{
	public partial class ListParameterPanel : ListParameterPanelDummy, ISyntaxHighlighted, ISearchableContainer
	{
		private int _contentHash;

		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		public HashSet<string> Collection
		{
			get
			{
				var list = Utility.ListFromCommaSeparatedString(textBox.Text)
					.Distinct(StringComparer.Create(CultureInfo.InvariantCulture, true));
				return new HashSet<string>(list);
			}
		}

		public ListParameterPanel()
		{
			InitializeComponent();

			textBox.richTextBox.syntaxFlags = RichTextBoxEx.SyntaxFlags.Limited;
			textBox.richTextBox.ValueChanged += OnValueChanged;
			textBox.richTextBox.EnterPressed += TextBox_EnterPressed;
			textBox.richTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
			textBox.richTextBox.KeyPress += TextBox_KeyPress;
			textBox.richTextBox.GotFocus += TextBox_GotFocus;
			textBox.richTextBox.LostFocus += TextBox_LostFocus;

			FontChanged += FontDidChange;
		}

		protected void FontDidChange(object sender, EventArgs e)
		{
			WhileIgnoringEvents(() => {
				textBox.Font = this.Font;
			});
		}

		protected override void OnSetParameter()
		{
			// Text box
			textBox.Placeholder = parameter.placeholder ?? "Enter items separated by commas";
			textBox.Enabled = parameter.isEnabled || !parameter.isOptional;
			textBox.InitUndo();

			// Enabled checkbox
			cbEnabled.Enabled = parameter.isOptional;
			cbEnabled.Checked = parameter.isEnabled;

			// Tooltip
			SetTooltip(label, textBox);
		}

		protected override void OnRefreshValue()
		{
			textBox.SetTextSilent(Utility.ListToCommaSeparatedString(this.parameter.value));
			cbEnabled.Checked = this.parameter.isEnabled;
		}

		private void TextBox_GotFocus(object sender, EventArgs e)
		{
			if (isIgnoringEvents || !Enabled)
				return;

			_contentHash = textBox.Text.GetHashCode();
		}

		private void TextBox_LostFocus(object sender, EventArgs e)
		{
			if (isIgnoringEvents || !Enabled)
				return;

			// Clean up
			WhileIgnoringEvents(() => {
				string text = Utility.ListToCommaSeparatedString(Collection);
				if (textBox.Text != text)
					textBox.Text = text;
			});

			var newContentHash = textBox.Text.GetHashCode();
			if (_contentHash != newContentHash)
			{
				_contentHash = newContentHash;
				NotifyValueChanged(_contentHash);
			}
		}

		private void TextBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyData == Keys.Return || e.KeyData == Keys.Enter)
			{
				if (Collection.Count > 0)
				{
					WhileIgnoringEvents(() => {
						textBox.Text = Utility.ListToCommaSeparatedString(Collection) + ", ";
						textBox.richTextBox.RefreshSyntaxHighlight(true); // Rehighlight
						textBox.Select(textBox.Text.Length);
					});
				}
				e.IsInputKey = false;
			}
		}

		private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == ',')
			{
				if (Collection.Count > 0 && textBox.Text.Substring(textBox.SelectionStart).ContainsNoneOf(c => char.IsWhiteSpace(c) == false))
				{
					WhileIgnoringEvents(() => {
						textBox.Text = Utility.ListToCommaSeparatedString(Collection) + ", ";
						textBox.richTextBox.RefreshSyntaxHighlight(true); // Rehighlight
						textBox.Select(textBox.Text.Length);
					});

					e.Handled = true;
				}
			}
		}

		protected override void OnSetEnabled(bool bEnabled)
		{
			cbEnabled.Enabled = bEnabled && parameter.isOptional;
			textBox.Enabled = bEnabled && parameter.isEnabled;
		}

		protected override void OnSetReserved(bool bReserved, string reservedValue)
		{
			cbEnabled.Enabled = !bReserved && parameter.isOptional;
			textBox.Enabled = !bReserved && parameter.isEnabled;

			WhileIgnoringEvents(() => {
				if (bReserved)
					textBox.Text = reservedValue;
				else
				{
					textBox.Text = Utility.ListToCommaSeparatedString(Collection);
					textBox.richTextBox.RefreshSyntaxHighlight(false); // Rehighlight
				}
				textBox.InitUndo();
			});
		}

		private void CbEnabled_CheckedChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			textBox.Enabled = cbEnabled.Checked || !parameter.isOptional;
			if (isIgnoringEvents)
				return;

			parameter.isEnabled = cbEnabled.Checked || !parameter.isOptional;
			NotifyEnabledChanged();
		}

		private void OnValueChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			this.parameter.value = Collection;
			Current.IsFileDirty = true;
		}

		private void TextBox_EnterPressed(object sender, EventArgs e)
		{
			if (isIgnoringEvents || Enabled == false)
				return;

			this.parameter.value = Collection;
			Current.IsDirty = true;
		}

		public ISearchable[] GetSearchables()
		{
			return new ISearchable[] { textBox.richTextBox };
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			SizeLabel(label);
			if (textBox != null)
				SizeToWidth(textBox);
		}

		public void RefreshSyntaxHighlight(bool immediate, bool invalidate)
		{
			if (invalidate)
				textBox.richTextBox.InvalidateSyntaxHighlighting();
			textBox.richTextBox.RefreshSyntaxHighlight(immediate);
		}
	}
}
