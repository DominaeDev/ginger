using Ginger.Integration;
using Ginger.Properties;
using System;
using System.Globalization;
using System.Windows.Forms;

namespace Ginger
{
	public partial class EditModelSettingsDialog : FormEx
	{
		public ChatParameters Parameters { get; private set; }
		private ChatParameters _defaultParameters;

		private bool _bIgnoreEvents = false;

		private bool CanEdit { get { return cbPresets.SelectedIndex != 1; } }
		private bool CanSave { get { return cbPresets.SelectedIndex >= 2; } }

		public EditModelSettingsDialog(ChatParameters defaultParameters = null)
		{
			if (defaultParameters != null)
				_defaultParameters = defaultParameters.Copy();
			else
				_defaultParameters = AppSettings.BackyardSettings.UserSettings.Copy();
			Parameters = _defaultParameters.Copy();

			InitializeComponent();

			Load += EditModelSettingsDialog_Load;

			CancelButton = btnCancel;

			textBox_Temperature.KeyPress += TextBox_Decimal_KeyPress;
			textBox_MinP.KeyPress += TextBox_Decimal_KeyPress;
			textBox_TopP.KeyPress += TextBox_Decimal_KeyPress;
			textBox_TopK.KeyPress += TextBox_Integer_KeyPress;
			textBox_RepeatPenalty.KeyPress += TextBox_Decimal_KeyPress;
			textBox_RepeatTokens.KeyPress += TextBox_Integer_KeyPress;
			textBox_Temperature.LostFocus += TextBox_Temperature_LostFocus;
			textBox_MinP.LostFocus += TextBox_MinP_LostFocus;
			textBox_TopP.LostFocus += TextBox_TopP_LostFocus;
			textBox_TopK.LostFocus += TextBox_TopK_LostFocus;
			textBox_RepeatPenalty.LostFocus += TextBox_RepeatPenalty_LostFocus;
			textBox_RepeatTokens.LostFocus += TextBox_RepeatTokens_LostFocus;

			cbSampling.SelectedIndexChanged += CbSampling_SelectedIndexChanged;

			SetToolTip(labelPromptTemplate, Resources.tooltip_model_prompt_template);
			SetToolTip(labelSampling, Resources.tooltip_model_sampler);
			SetToolTip(labelTemperature, Resources.tooltip_model_temperature);
			SetToolTip(labelMinP, Resources.tooltip_model_minp);
			SetToolTip(labelTopP, Resources.tooltip_model_topp);
			SetToolTip(labelTopK, Resources.tooltip_model_topk);
			SetToolTip(labelRepeatPenalty, Resources.tooltip_model_repeat_penalty);
			SetToolTip(labelPenaltyTokens, Resources.tooltip_model_repeat_lastn);
		}

		private void EditModelSettingsDialog_Load(object sender, EventArgs e)
		{
			WhileIgnoringEvents(() => {
				cbPresets.BeginUpdate();
				cbPresets.Items.Add("");
				cbPresets.Items.Add("(Backyard AI Defaults)");
				foreach (var preset in AppSettings.BackyardSettings.Presets)
					cbPresets.Items.Add(preset.name);

				cbPresets.SelectedItem = cbPresets.Items[0];
				cbPresets.EndUpdate();

				cbModel.BeginUpdate();
				cbModel.Items.Add("(Default model)");
				if (Backyard.Models != null)
				{
					foreach (var model in Backyard.Models)
						cbModel.Items.Add(model);
				}
				cbModel.SelectedItem = cbModel.Items[0];
				cbModel.EndUpdate();

				cbPromptTemplate.BeginUpdate();
				cbPromptTemplate.Items.AddRange(new string[] {
					"(Model default)",
					"Generic",
					"ChatML",
					"Llama 3",
					"Gemma 2",
					"Command-R",
					"Mistral Instruct",
				});
				cbPromptTemplate.EndUpdate();

				cbSampling.BeginUpdate();
				cbSampling.Items.AddRange(new string[] {
					"Min-P",
					"Top-K"
				});
				cbSampling.EndUpdate();

				cbSavePromptTemplate.Checked = AppSettings.BackyardLink.SavePromptTemplates;
			});

			RefreshPresetButtons();
			RefreshValues();

			cbModel.Focus();
			cbModel.Select();
		}

		private void WhileIgnoringEvents(Action action)
		{
			bool bIgnoring = !_bIgnoreEvents;
			_bIgnoreEvents = true;
			action.Invoke();
			if (bIgnoring)
				_bIgnoreEvents = false;
		}

		private void RefreshValues()
		{
			WhileIgnoringEvents(() => {
				if (string.IsNullOrEmpty(Parameters.model) || Backyard.Models == null)
					cbModel.SelectedIndex = 0;
				else
				{
					int idxModel = Array.FindIndex(Backyard.Models, fn => string.Compare(fn, Parameters.model, StringComparison.OrdinalIgnoreCase) == 0);

					if (idxModel >= 0 && idxModel < Backyard.Models.Length && idxModel < cbModel.Items.Count - 1)
						cbModel.SelectedIndex = idxModel + 1;
					else
						cbModel.SelectedIndex = 0;
				}

				if (cbPresets.SelectedIndex == 1) // Backyard defaults
				{
					// Disable prompt template
					cbPromptTemplate.SelectedIndex = 0;
					labelPromptTemplate.Enabled = false;
					panelPromptTemplate.Enabled = false;
				}
				else
				{
					// Enable prompt template
					cbPromptTemplate.SelectedItem = cbPromptTemplate.Items[Parameters.iPromptTemplate];
					labelPromptTemplate.Enabled = true;
					panelPromptTemplate.Enabled = true;
				}

				Set(textBox_Temperature, Parameters.temperature);
				Set(textBox_MinP, Parameters.minP);
				Set(textBox_TopP, Parameters.topP);
				Set(textBox_TopK, Parameters.topK);
				Set(textBox_RepeatPenalty, Parameters.repeatPenalty);
				Set(textBox_RepeatTokens, Parameters.repeatLastN);
				Set(trackBar_Temperature, Convert.ToInt32(Parameters.temperature * 10));
				Set(trackBar_MinP, Convert.ToInt32(Parameters.minP * 100));
				Set(trackBar_TopP, Convert.ToInt32(Parameters.topP * 100));
				Set(trackBar_TopK, Convert.ToInt32(Parameters.topK));
				Set(trackBar_RepeatPenalty, Convert.ToInt32(Parameters.repeatPenalty * 100));
				Set(trackBar_PenaltyTokens, Convert.ToInt32(Parameters.repeatLastN));

                if (Parameters.minPEnabled)
                    cbSampling.SelectedIndex = 0; // Min-P
                else
                    cbSampling.SelectedIndex = 1; // Top-K
				
				ToggleControls();
			});
		}

		private void Set(TextBoxBase textBox, decimal value)
		{
			textBox.Text = value.ToString(CultureInfo.InvariantCulture);
		}

		private void Set(TrackBarEx trackBar, int value)
		{
			trackBar.Value = Math.Min(Math.Max(value, trackBar.Minimum), trackBar.Maximum);
		}

		private void CbSampling_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			Parameters.minPEnabled = cbSampling.SelectedIndex == 0;
			ToggleControls();
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void trackBar_Temperature_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value = Utility.RoundNearest(trackBar_Temperature.Value * 0.1m, 0.1m);
			textBox_Temperature.Text = value.ToString("0.0", CultureInfo.InvariantCulture);
		}

		private void trackBar_MinP_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value = Utility.RoundNearest(trackBar_MinP.Value * 0.01m, 0.05m);
			textBox_MinP.Text = value.ToString("0.0#", CultureInfo.InvariantCulture);
		}

		private void trackBar_TopP_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value = Utility.RoundNearest(trackBar_TopP.Value * 0.01m, 0.05m);
			textBox_TopP.Text = value.ToString("0.0#", CultureInfo.InvariantCulture);
		}

		private void trackBar_TopK_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int value = trackBar_TopK.Value;
			textBox_TopK.Text = value.ToString();
		}

		private void trackBar_RepeatPenalty_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value = Utility.RoundNearest(trackBar_RepeatPenalty.Value * 0.01m, 0.05m);
			textBox_RepeatPenalty.Text = value.ToString("0.0#", CultureInfo.InvariantCulture);
		}

		private void trackBar_RepeatTokens_ValueChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int value = trackBar_PenaltyTokens.Value;
			textBox_RepeatTokens.Text = value.ToString();
		}

		private void TextBox_Integer_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			if (e.KeyChar == 0x7f) // Ctrl+Backspace
			{
				e.Handled = true;
				return;
			}

			if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar))
				e.Handled = false; //Do not reject the input
			else
				e.Handled = true; //Reject the input
		}

		private void TextBox_Decimal_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			if (e.KeyChar == 0x7f) // Ctrl+Backspace
			{
				e.Handled = true;
				return;
			}

			if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar) || e.KeyChar == '.')
			{
				var textBox = sender as TextBoxBase;
				if (e.KeyChar == '.' && textBox != null && textBox.Text.IndexOf('.') != -1)
				{
					e.Handled = true; // Allow only one point
					return;
				}

				e.Handled = false; // Do not reject the input
			}
			else
			{
				e.Handled = true; // Reject the input
			}
		}

		private void SetToolTip(Control control, string text)
		{
			if (this.components == null)
				this.components = new System.ComponentModel.Container();

			var toolTip = new ToolTip(this.components);
			toolTip.SetToolTip(control, text);
			toolTip.UseFading = false;
			toolTip.UseAnimation = false;
			toolTip.AutomaticDelay = 250;
			toolTip.AutoPopDelay = 3500;
		}

		private void cbPresets_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int index = cbPresets.SelectedIndex;

			if (index == 0)
				Parameters = _defaultParameters.Copy();
			else if (index == 1)
				Parameters = ChatParameters.Default;
			else if (index >= 2 && index < AppSettings.BackyardSettings.Presets.Count + 2)
				Parameters = AppSettings.BackyardSettings.Presets[index - 2].Copy();

			RefreshPresetButtons();
			RefreshValues();
			ToggleControls();
		}

		private void ToggleControls()
		{
			bool bMinPEnabled = Parameters.minPEnabled;
			bool bDefaultModel = string.IsNullOrEmpty(Parameters.model);

			labelModel.Enabled = CanEdit;
			labelPromptTemplate.Enabled = CanEdit && !bDefaultModel;
			labelTemperature.Enabled = CanEdit;
			labelSampling.Enabled = CanEdit;
			labelMinP.Enabled = CanEdit && bMinPEnabled;
			labelTopP.Enabled = CanEdit && !bMinPEnabled;
			labelTopK.Enabled = CanEdit && !bMinPEnabled;
			labelRepeatPenalty.Enabled = CanEdit;
			labelPenaltyTokens.Enabled = CanEdit;
			panelModel.Enabled = CanEdit;
			panelPromptTemplate.Enabled = CanEdit && !bDefaultModel;
			panelTemperature.Enabled = CanEdit;
			panelSampling.Enabled = CanEdit;
			panelMinP.Enabled = CanEdit && bMinPEnabled;
			panelTopP.Enabled = CanEdit && !bMinPEnabled;
			panelTopK.Enabled = CanEdit && !bMinPEnabled;
			panelRepeatPenalty.Enabled = CanEdit;
			panelPenaltyTokens.Enabled = CanEdit;
			trackBar_Temperature.Visible = CanEdit;
			trackBar_MinP.Visible = CanEdit && bMinPEnabled;
			trackBar_TopP.Visible = CanEdit && !bMinPEnabled;
			trackBar_TopK.Visible = CanEdit && !bMinPEnabled;
			trackBar_RepeatPenalty.Visible = CanEdit;
			trackBar_PenaltyTokens.Visible = CanEdit;
		}

		private void cbModel_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int index = cbModel.SelectedIndex;
			if (index == 0)
			{
				Parameters.model = null;

				WhileIgnoringEvents(() => {
					// Disable prompt template
					cbPromptTemplate.SelectedIndex = 0;
					labelPromptTemplate.Enabled = false;
					panelPromptTemplate.Enabled = false;
				});
			}
			else
			{
				Parameters.model = cbModel.SelectedItem as string;

				WhileIgnoringEvents(() => {
					// Enable prompt template
					labelPromptTemplate.Enabled = true;
					panelPromptTemplate.Enabled = true;

					if (AppSettings.BackyardLink.SavePromptTemplates)
					{
						int iPromptTemplate = AppSettings.BackyardSettings.GetPromptTemplateForModel(Parameters.model);
						if (iPromptTemplate != -1)
							cbPromptTemplate.SelectedIndex = iPromptTemplate;
					}
				});
			}

		}

		private void cbPromptTemplate_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int idxPromptTemplate = cbPromptTemplate.SelectedIndex;
			Parameters.iPromptTemplate = idxPromptTemplate;

			if (AppSettings.BackyardLink.SavePromptTemplates)
				AppSettings.BackyardSettings.SetPromptTemplateForModel(Parameters.model, Parameters.iPromptTemplate);
		}

		private void textBox_Temperature_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value;
			if (decimal.TryParse(textBox_Temperature.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
				value = Math.Min(Math.Max(value, 0m), 5m);
			else
				value = 0m;

			Parameters.temperature = value;

			WhileIgnoringEvents(() => {
				Set(trackBar_Temperature, Convert.ToInt32(value * 10));
			});
		}

		private void textBox_MinP_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value;
			if (decimal.TryParse(textBox_MinP.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
				value = Math.Min(Math.Max(value, 0m), 1m);
			else
				value = 0m;

			Parameters.minP = value;

			WhileIgnoringEvents(() => {
				Set(trackBar_MinP, Convert.ToInt32(value * 100));
			});
		}

		private void textBox_TopP_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value;
			if (decimal.TryParse(textBox_TopP.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
				value = Math.Min(Math.Max(value, 0m), 1m);
			else
				value = 0m;

			Parameters.topP = value;

			WhileIgnoringEvents(() => {
				Set(trackBar_TopP, Convert.ToInt32(value * 100));
			});
		}

		private void textBox_TopK_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int value;
			if (int.TryParse(textBox_TopK.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
				value = Math.Min(Math.Max(value, 0), 100);
			else
				value = 0;

			Parameters.topK = value;

			WhileIgnoringEvents(() => {
				Set(trackBar_TopK, value);
			});
		}

		private void textBox_RepeatPenalty_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			decimal value;
			if (decimal.TryParse(textBox_RepeatPenalty.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
				value = Math.Min(Math.Max(value, 0m), 2m);
			else
				value = 0m;

			Parameters.repeatPenalty = value;

			WhileIgnoringEvents(() => {
				Set(trackBar_RepeatPenalty, Convert.ToInt32(value * 100));
			});
		}

		private void textBox_RepeatTokens_TextChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int value;
			if (int.TryParse(textBox_RepeatTokens.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
				value = Math.Min(Math.Max(value, 16), 512);
			else
				value = 16;

			Parameters.repeatLastN = value;

			WhileIgnoringEvents(() => {
				Set(trackBar_PenaltyTokens, value);
			});
		}

		private void TextBox_Temperature_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			WhileIgnoringEvents(() => {
				textBox_Temperature.Text = Parameters.temperature.ToString(CultureInfo.InvariantCulture);
			});
		}

		private void TextBox_MinP_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			WhileIgnoringEvents(() => {
				textBox_MinP.Text = Parameters.minP.ToString(CultureInfo.InvariantCulture);
			});
		}

		private void TextBox_TopP_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			WhileIgnoringEvents(() => {
				textBox_TopP.Text = Parameters.topP.ToString(CultureInfo.InvariantCulture);
			});
		}

		private void TextBox_TopK_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			WhileIgnoringEvents(() => {
				textBox_TopK.Text = Parameters.topK.ToString();
			});
		}

		private void TextBox_RepeatPenalty_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			WhileIgnoringEvents(() => {
				textBox_RepeatPenalty.Text = Parameters.repeatPenalty.ToString(CultureInfo.InvariantCulture);
			});
		}

		private void TextBox_RepeatTokens_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			WhileIgnoringEvents(() => {
				textBox_RepeatTokens.Text = Parameters.repeatLastN.ToString();
			});
		}

		private void btnCopy_Click(object sender, EventArgs e)
		{
			Clipboard.SetDataObject(ChatParametersClipboard.FromParameters(Parameters), false);
			RefreshPresetButtons();
		}

		private void btnPaste_Click(object sender, EventArgs e)
		{
			if (Clipboard.ContainsData(ChatParametersClipboard.Format) == false)
				return;

			ChatParametersClipboard clip = Clipboard.GetData(ChatParametersClipboard.Format) as ChatParametersClipboard;
			if (clip == null || clip.parameters == null)
				return;

			Parameters = clip.parameters.Copy();
			RefreshValues();
		}

		private void btnSavePreset_Click(object sender, EventArgs e)
		{
			int index = cbPresets.SelectedIndex;
			if (index <= 0)
				return;
			else // Preset
			{
				index -= 2;
				if (index >= 0 && index < AppSettings.BackyardSettings.Presets.Count)
					AppSettings.BackyardSettings.Presets[index] = (ChatParameters)Parameters.Clone();
			}
		}

		private void btnNewPreset_Click(object sender, EventArgs e)
		{
			var dlg = new EnterNameDialog();
			dlg.Text = "Please enter preset name";
			dlg.Label = "Preset name";
			if (dlg.ShowDialog() != DialogResult.OK)
				return;

			string presetName = dlg.Value;
			if (string.IsNullOrEmpty(presetName))
				return;

			int idxPreset = AppSettings.BackyardSettings.Presets.FindIndex(p => string.Compare(p.name, presetName, StringComparison.InvariantCultureIgnoreCase) == 0);

			if (idxPreset != -1)
			{
				MessageBox.Show("A preset with this name already exists.", "Save preset", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			var preset = (ChatParameters)Parameters.Clone();
			preset.name = presetName;

			AppSettings.BackyardSettings.Presets.Add(preset);

			WhileIgnoringEvents(() => {
				cbPresets.BeginUpdate();
				cbPresets.Items.Add(presetName);
				cbPresets.SelectedIndex = cbPresets.Items.Count - 1;
				cbPresets.EndUpdate();
			});

			RefreshPresetButtons();
			RefreshValues();
		}

		private void btnRemovePreset_Click(object sender, EventArgs e)
		{
			int index = cbPresets.SelectedIndex;
			if (index < 2)
				return;

			var mr = MessageBox.Show("Remove this preset?", Resources.cap_confirm, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
			if (mr != DialogResult.Yes)
				return;

			index -= 2;
			if (index >= 0 && index < AppSettings.BackyardSettings.Presets.Count)
				AppSettings.BackyardSettings.Presets.RemoveAt(index);

			WhileIgnoringEvents(() => {
				cbPresets.BeginUpdate();
				cbPresets.Items.RemoveAt(cbPresets.SelectedIndex);
				cbPresets.SelectedIndex = 0;
				cbPresets.EndUpdate();
			});

			Parameters = _defaultParameters.Copy();

			RefreshPresetButtons();
			RefreshValues();
		}

		private void RefreshPresetButtons()
		{
			btnNewPreset.Enabled = true;
			btnSavePreset.Enabled = CanSave;
			btnRemovePreset.Enabled = CanSave;

			btnCopy.Enabled = true;
			btnPaste.Enabled = CanEdit && Clipboard.ContainsData(ChatParametersClipboard.Format);
		}

		private void cbSavePromptTemplate_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			AppSettings.BackyardLink.SavePromptTemplates = cbSavePromptTemplate.Checked;

			if (cbSavePromptTemplate.Checked)
			{
				WhileIgnoringEvents(() => {
					int iPromptTemplate = AppSettings.BackyardSettings.GetPromptTemplateForModel(Parameters.model);
					if (iPromptTemplate != -1)
						cbPromptTemplate.SelectedIndex = iPromptTemplate;
				});
			}
		}
	}
}
