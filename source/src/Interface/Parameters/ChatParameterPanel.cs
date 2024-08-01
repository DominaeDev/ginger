using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Ginger
{
	public partial class ChatParameterPanel : ChatParameterPanelDummy, IFlexibleParameterPanel
	{
		public bool FlexibleHeight { get; set; }

		protected override CheckBox parameterCheckBox { get { return cbEnabled; } }
		protected override Label parameterLabel { get { return label; } }

		public ChatParameterPanel()
		{
			InitializeComponent();

			textBox.Size = new Size(textBox.Size.Width, 72);

			Init(label, textBox, cbEnabled, btnWrite);

			Resize += OnResize;
			Load += ChatParameterPanel_Load;
		}

		private void BtnAddEntry_Click(object sender, EventArgs e)
		{
			MainForm.EnableFormLevelDoubleBuffering(true);

			textBox.Suspend();
			var sbText = new StringBuilder(textBox.Text);
			sbText.Trim();
			textBox.Text = sbText.ToString();
			textBox.Focus();

			int position = sbText.Length;

			string characterName = Current.Character.namePlaceholder;
			string userName = Current.Card.userPlaceholder;
			var sbAppend = new StringBuilder();
			if (sbText.Length > 0)
			{
				sbAppend.AppendLine();
				sbAppend.AppendLine();
				position += 2;
			}
			sbAppend.AppendFormat("{0}: \"\"\r\n{1}: \"\"", userName, characterName);

			textBox.AppendText(sbAppend.ToString());
			textBox.Select(position + userName.Length + 3, 0);
			textBox.Resume();
			textBox.richTextBox.RefreshSyntaxHighlight(true);
		}

		private void ChatParameterPanel_Load(object sender, EventArgs e)
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

		private void OnResize(object sender, System.EventArgs e)
		{
			if (isIgnoringEvents)
				return;

			RefreshLineWidth();
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
				textBox.richTextBox.RightMargin = 0;
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
				bottomPanel.Location = new Point(0, textBox.Location.Y + height + 4);
				textBox.Invalidate(); // Repaint (to avoid border artifacts)
				NotifySizeChanged(); // Notify parent the size has changed
			}
		}

		protected override void OnSetParameter()
		{
			base.OnSetParameter();
//			RefreshFlexibleSize();
		}

		protected override void OnSetEnabled(bool bEnabled)
		{
			btnAddEntry.Enabled = bEnabled;
			base.OnSetEnabled(bEnabled);
		}

		private void OnMouseClick(object sender, MouseEventArgs e)
		{
			ParameterPanel_MouseClick(sender, e);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			var scaleFactor = this.Font.SizeInPoints / Constants.ReferenceFontSize;
			label.Bounds = new Rectangle(2,3,
				Convert.ToInt32(Constants.ParameterPanel.LabelWidth * scaleFactor - 3),
				Convert.ToInt32(this.Size.Height - bottomPanel.Height - 4));

			if (textBox != null)
				SizeToWidth(textBox);
			bottomPanel.Bounds = new Rectangle(0, textBox.Location.Y + textBox.Size.Height + 4, this.Width, bottomPanel.Height);
		}

		public override int GetParameterHeight()
		{
			return textBox.Location.Y + textBox.Height + bottomPanel.Height;
		}
	}
}
