namespace Ginger
{
	partial class PortraitPreview
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
			this.SuspendLayout();
			// 
			// PortraitPreview
			// 
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.PortraitPreview_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.PortraitPreview_DragEnter);
			this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PortraitPreview_MouseClick);
			this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.PortraitPreview_MouseDoubleClick);
			((System.ComponentModel.ISupportInitialize)(this)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
	}
}
