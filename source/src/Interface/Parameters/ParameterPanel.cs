using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ginger
{
	public class ParameterPanel<T> : UserControl, IParameterPanel where T : class, IParameter
	{
		protected T parameter;

		public bool isActive // Is parameter visible? (Control.Visible depends on parent)
		{
			get { return _bActive; } 
			set { _bActive = value; Visible = _bActive; }
		}
		private bool _bActive = true;

		protected virtual CheckBox parameterCheckBox { get { return null; } }
		protected virtual Label parameterLabel { get { return null; } }

		protected bool isIgnoringEvents { get { return _bIgnoreEvents || isReserved; } }
		private bool _bIgnoreEvents = false;
		protected bool isNotifyingValueChanged { get; private set; }

		public bool isReserved { get; private set; }

		public event EventHandler<ParameterEventArgs> ParameterValueChanged;
		public event EventHandler ParameterEnabledChanged;
		public event EventHandler ParameterResized; // Parameter size changed
		public event MouseEventHandler OnRightClick;

		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		public ParameterPanel()
		{
			this.DoubleBuffered = true;
			this.SetStyle(ControlStyles.DoubleBuffer, true);
		}

		public IParameter GetParameter()
		{
			return parameter;
		}

		public void SetParameter(IParameter parameter)
		{
			this.parameter = (T)parameter;

			SetLabel(parameter.label);
			SetTooltip(parameter.description);

			_bIgnoreEvents = true;
			if (parameterCheckBox != null)
			{
				parameterCheckBox.Enabled = parameter.isOptional;
				parameterCheckBox.Checked = parameter.isEnabled;
			}

			 OnSetParameter();
			_bIgnoreEvents = false;
		}

		public void ResetParameterReference(IParameter parameter)
		{
			this.parameter = (T)parameter;
			RefreshValue();
		}

		protected virtual void OnSetParameter() { }
		protected virtual void OnRefreshValue() {}

		public void SetEnabled(bool bEnabled) 
		{
			_bIgnoreEvents = true;
			OnSetEnabled(bEnabled);
			_bIgnoreEvents = false;
		}

		protected virtual void OnSetEnabled(bool bEnabled) { }

		public void SetReserved(bool bReserved, string value) 
		{
			_bIgnoreEvents = true;
			OnSetReserved(bReserved, bReserved ? value : default(string));
			_bIgnoreEvents = false;

			isReserved = bReserved;
		}

		protected virtual void OnSetReserved(bool bEnabled, string value) { }

		public void RefreshValue()
		{
			_bIgnoreEvents = true;
			OnRefreshValue();
			_bIgnoreEvents = false;
		}

		protected void NotifyValueChanged(int hash)
		{
			NotifyValueChanged(hash.ToString());
		}

		protected void NotifyValueChanged(StringHandle subId = default(StringHandle))
		{
			if (isNotifyingValueChanged)
				return;
			isNotifyingValueChanged = true;
			ParameterValueChanged?.Invoke(this, new ParameterEventArgs() {
				subId = subId,
			});
			isNotifyingValueChanged = false;
			Current.IsDirty = true;
		}

		protected void NotifyEnabledChanged()
		{
			ParameterEnabledChanged?.Invoke(this, EventArgs.Empty);
			Current.IsDirty = true;
		}

		protected void SetTooltip(params Control[] controls)
		{
			if (string.IsNullOrEmpty(this.parameter.description))
				return;

			if (this.components == null)
			{
				this.components = new System.ComponentModel.Container();
			}

			var toolTip = new ToolTip(this.components);
			toolTip.UseFading = false;
			toolTip.UseAnimation = false;
			toolTip.AutomaticDelay = 250;
			toolTip.AutoPopDelay = 3500;

			foreach (var control in controls)
			{
				toolTip.SetToolTip(control, this.parameter.description);
			}
		}

		protected void SetTooltip(string tooltip, params Control[] controls)
		{
			if (string.IsNullOrEmpty(tooltip))
				return;

			if (this.components == null)
			{
				this.components = new System.ComponentModel.Container();
			}

			var toolTip = new ToolTip(this.components);
			toolTip.UseFading = false;
			toolTip.UseAnimation = false;
			toolTip.AutomaticDelay = 250;
			toolTip.AutoPopDelay = 3500;

			foreach (var control in controls)
				toolTip.SetToolTip(control, tooltip);
		}

		protected void WhileIgnoringEvents(Action action)
		{
			if (_bIgnoreEvents)
				action.Invoke();
			else
			{
				_bIgnoreEvents = true;
				action.Invoke();
				_bIgnoreEvents = false;
			}
		}

		protected void NotifySizeChanged()
		{
			Invalidate();
			ParameterResized?.Invoke(this, EventArgs.Empty);
		}

		protected void ParameterPanel_MouseClick(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Right)
				OnRightClick?.Invoke(this, args);
		}

		public void SetLabel(string label)
		{
			if (parameterLabel != null)
				parameterLabel.Text = Utility.EscapeMenu(label ?? "");
		}

		public virtual int GetParameterHeight()
		{
			return 29;
		}

		protected void SizeLabel(Label label)
		{
			var scaleFactor = this.Font.SizeInPoints / Constants.ReferenceFontSize;

			label.Bounds = new Rectangle(
				2,
				3,
				Convert.ToInt32(Constants.ParameterPanel.LabelWidth * scaleFactor - 3),
				Convert.ToInt32(this.Size.Height - 4));
		}

		protected void SizeToWidth(Control control)
		{
			var scaleFactor = this.Font.SizeInPoints / Constants.ReferenceFontSize;

			control.Bounds = new Rectangle(
				Convert.ToInt32(Constants.ParameterPanel.LabelWidth * scaleFactor),
				control.Location.Y,
				Convert.ToInt32((this.Size.Width - (Constants.ParameterPanel.LabelWidth + Constants.ParameterPanel.CheckboxWidth) * scaleFactor)),
				control.Size.Height);
		}

		protected void SizeToWidth(Control control, int maxWidth)
		{
			var scaleFactor = this.Font.SizeInPoints / Constants.ReferenceFontSize;

			control.Bounds = new Rectangle(
				Convert.ToInt32(Constants.ParameterPanel.LabelWidth * scaleFactor),
				control.Location.Y,
				Convert.ToInt32(Math.Min(maxWidth, (this.Size.Width - (Constants.ParameterPanel.LabelWidth + Constants.ParameterPanel.CheckboxWidth)) * scaleFactor)),
				control.Size.Height);
		}

		protected void MoveValueControl(Control control)
		{
			var scaleFactor = this.Font.SizeInPoints / Constants.ReferenceFontSize;

			control.Location = new Point(
				Convert.ToInt32(Constants.ParameterPanel.LabelWidth * scaleFactor),
				control.Location.Y);
		}
	}

	public interface IParameterPanel
	{
		IParameter GetParameter();
		void SetLabel(string label);
		void SetEnabled(bool bEnabled);
		void SetReserved(bool bReserved, string value = null);
		void SetParameter(IParameter parameter);
		void ResetParameterReference(IParameter parameter);
		void RefreshValue();
		int GetParameterHeight();
		
		bool isActive { get; set; }
		bool isReserved { get; }
	}

	public interface IFlexibleParameterPanel
	{
		void RefreshFlexibleSize();
		void RefreshLineWidth();
	}

	public class ParameterEventArgs : EventArgs
	{
		public StringHandle subId { get; set; }
	}

}
