using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public partial class MultiTextParameterPanel : MultiTextParameterPanelDummy, IFlexibleParameterPanel, IVisualThemed
	{
		public enum TextBoxSize
		{
			Short,		// 4
			Brief,		// 8
			Component,	// 13
		}

		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }
		public bool FlexibleHeight { get; set; }

		public MultiTextParameterPanel()
		{
			InitializeComponent();

			Init(label, textBox, cbEnabled, btnWrite);

			Resize += TextBox_Resize;
			Load += MultiTextParameterPanel_Load;
		}

		private void MultiTextParameterPanel_Load(object sender, EventArgs e)
		{
			textBox.TextSizeChanged += TextBox_TextSizeChanged;
		}

		private void TextBox_TextSizeChanged(object sender, EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			if (FlexibleHeight)
				RefreshFlexibleSize();
		}

		private void TextBox_Resize(object sender, System.EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			RefreshLineWidth();
		}

		public MultiTextParameterPanel(TextBoxSize size) : this()
		{
			// Set text box height
			int height;
			switch (size)
			{
			default:
			case TextBoxSize.Short:
				height = 72;
				break;
			case TextBoxSize.Brief:
				height = 130;
				break;
			case TextBoxSize.Component:
				height = 130;
//				height = 201; //158;
				break;
			}

			textBox.Size = new Size(textBox.Size.Width, height);
		}

		public void RefreshLineWidth()
		{
			if (AppSettings.Settings.AutoBreakLine)
			{
				richTextBox.RightMargin = Math.Min((int)Math.Round(Constants.AutoWrapWidth * richTextBox.Font.SizeInPoints), Math.Max(richTextBox.Size.Width - 26, 0));
				richTextBox.WordWrap = true;
			}
			else
			{
				textBox.richTextBox.RightMargin = Math.Max(richTextBox.Size.Width - 26, 0); // Account for scrollbar
				textBox.richTextBox.WordWrap = true;
			}

			if (richTextBox.Multiline)
				richTextBox.SetInnerMargins(3, 2, 2, 0);
		}

		public void RefreshFlexibleSize()
		{
			if (FlexibleHeight == false || AllowFlexibleHeight == false)
				return;

			int height = textBox.TextSize.Height;
			height += 16; // Padding
			height = Math.Min(Math.Max(height, 94), 412); // Clamp

			if (textBox.Size.Height != height)
			{
				textBox.Size = new Size(textBox.Size.Width, height);
				this.Size = new Size(this.Size.Width, GetParameterHeight());
				textBox.Invalidate(); // Repaint (to avoid border artifacts)
				NotifySizeChanged(); // Notify parent the size has changed
			}
		}

		protected override void OnSetParameter()
		{
			base.OnSetParameter();
			RefreshFlexibleSize();
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		public override int GetParameterHeight()
		{
			return textBox.Location.Y + textBox.Height;
		}

		public void ApplyVisualTheme()
		{
			btnWrite.Image = VisualTheme.Theme.Write;
		}
	}
}
