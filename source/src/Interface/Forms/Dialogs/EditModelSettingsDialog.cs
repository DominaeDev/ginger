using Ginger.Integration;
using Ginger.Properties;
using System;
using System.Globalization;
using System.Windows.Forms;

namespace Ginger
{
	using ChatParameters = Backyard.ChatParameters;

	public partial class EditModelSettingsDialog : FormEx
	{
		public ChatParameters Editing { get; set; }
		public ChatParameters Parameters { get; private set; }
		
		private ChatParameters _defaultParameters;

		private bool _bIgnoreEvents = false;

		private int idxUserSettings { get { return Editing != null ? 1 : 0; } }
		private int idxPresets { get { return Editing != null ? 2 : 1; } }

		private bool IsEditing { get { return Editing != null; } }
		private bool CanEdit { get { return true; } }
		private bool CanSave { get { return cbPresets.SelectedIndex >= idxPresets; } }

		private bool Dirty
		{
			set
			{
				if (value != btnSavePreset.Highlighted)
					btnSavePreset.Highlighted = value;
				_bDirty = value;
			}
		}
		private bool _bDirty = false;
		
		public EditModelSettingsDialog()
		{
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

			SetToolTip(labelModel, Resources.tooltip_model_model);
			SetToolTip(labelPromptTemplate, Resources.tooltip_model_prompt_template);
			SetToolTip(labelSampling, Resources.tooltip_model_sampler);
			SetToolTip(labelTemperature, Resources.tooltip_model_temperature);
			SetToolTip(labelMinP, Resources.tooltip_model_minp);
			SetToolTip(labelTopP, Resources.tooltip_model_topp);
			SetToolTip(labelTopK, Resources.tooltip_model_topk);
			SetToolTip(labelRepeatPenalty, Resources.tooltip_model_repeat_penalty);
			SetToolTip(labelPenaltyTokens, Resources.tooltip_model_repeat_lastn);
			SetToolTip(btnCopy, Resources.tooltip_link_copy_settings);
			SetToolTip(btnPaste, Resources.tooltip_link_paste_settings);
			SetToolTip(btnReset, Resources.tooltip_link_reset_settings);
			SetToolTip(cbSavePromptTemplate, Resources.tooltip_model_associate_prompt_template);
		}

		private void EditModelSettingsDialog_Load(object sender, EventArgs e)
		{
			if (Editing != null)
				_defaultParameters = Editing.Copy();
			else
				_defaultParameters = AppSettings.BackyardSettings.UserSettings.Copy();

			Parameters = _defaultParameters.Copy();

			WhileIgnoringEvents(() => {
				cbPresets.BeginUpdate();
				if (IsEditing)
					cbPresets.Items.Add("Current settings");
				cbPresets.Items.Add("Default settings");
				foreach (var preset in AppSettings.BackyardSettings.Presets)
					cbPresets.Items.Add(preset.name);

				cbPresets.SelectedItem = cbPresets.Items[0];
				cbPresets.EndUpdate();

				cbModel.BeginUpdate();
				cbModel.Items.Add("(Default model)");
				foreach (var model in BackyardModelDatabase.Models)
					cbModel.Items.Add(model);
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
				cbPromptTemplate.SelectedItem = cbPromptTemplate.Items[0];
				cbPromptTemplate.EndUpdate();

				cbSampling.BeginUpdate();
				cbSampling.Items.AddRange(new string[] {
					"Min-P",
					"Top-K"
				});
				cbSampling.EndUpdate();

//				labelPresets.Enabled = !EditingDefaults;
//				panelPresets.Enabled = !EditingDefaults;
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

				// Model
				bool bLocalModel = true;
				if (string.IsNullOrEmpty(Parameters.model))
					cbModel.SelectedIndex = 0;
				else
				{
					int idxModel = BackyardModelDatabase.Models.IndexOfAny(m => m.Compare(Parameters.model));

					if (idxModel >= 0 && idxModel < BackyardModelDatabase.Models.Count && idxModel < cbModel.Items.Count - 1)
					{
						var model = BackyardModelDatabase.Models[idxModel];
						cbModel.SelectedIndex = idxModel + 1;
						Parameters.model = model.id; // Validated
						bLocalModel = model.isCustomLocalModel;
					}
					else
					{
						// Unknown model
						cbModel.SelectedIndex = 0;
						Parameters.model = null;
					}
				}

				// Prompt template
				cbPromptTemplate.SelectedItem = cbPromptTemplate.Items[Parameters.iPromptTemplate];
				cbSavePromptTemplate.Enabled = bLocalModel;
				cbSavePromptTemplate.Checked = !bLocalModel || AppSettings.BackyardSettings.GetPromptTemplateForModel(Parameters.model) != -1;

				// Parameters
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

		private void cbPresets_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int index = cbPresets.SelectedIndex;

			if (IsEditing && index == 0)
				Parameters = _defaultParameters.Copy();
			else if (index == idxUserSettings)
				Parameters = AppSettings.BackyardSettings.UserSettings.Copy();
			else if (index >= idxPresets && index < AppSettings.BackyardSettings.Presets.Count + idxPresets)
				Parameters = AppSettings.BackyardSettings.Presets[index - idxPresets];

			RefreshPresetButtons();
			RefreshValues();
			ToggleControls();
			Dirty = false;
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
				Parameters.iPromptTemplate = 0;

				WhileIgnoringEvents(() => {
					// Disable prompt template
					cbPromptTemplate.SelectedIndex = 0;
					labelPromptTemplate.Enabled = false;
					panelPromptTemplate.Enabled = false;
				});
			}
			else
			{
				var model = (BackyardModel)cbModel.SelectedItem;
				Parameters.model = model.id;

				int iDefaultPromptTemplate = ChatParameters.PromptTemplateFromString(model.promptTemplate);

				WhileIgnoringEvents(() => {
					int iPromptTemplate;
					if (model.isCustomLocalModel)
					{
						iPromptTemplate = AppSettings.BackyardSettings.GetPromptTemplateForModel(Parameters.model);

						if (iPromptTemplate != -1)
						{
							cbPromptTemplate.SelectedIndex = iPromptTemplate;
							Parameters.iPromptTemplate = iPromptTemplate;

							cbSavePromptTemplate.Enabled = true;
							cbSavePromptTemplate.Checked = true;
						}
						else
						{
							cbPromptTemplate.SelectedIndex = iDefaultPromptTemplate;
							Parameters.iPromptTemplate = iDefaultPromptTemplate;
							cbSavePromptTemplate.Enabled = true;
							cbSavePromptTemplate.Checked = false;
						}
					}
					else // Set by Backyard
					{
						cbPromptTemplate.SelectedIndex = iDefaultPromptTemplate;
						Parameters.iPromptTemplate = iDefaultPromptTemplate;
						cbSavePromptTemplate.Enabled = false;
						cbSavePromptTemplate.Checked = true;
					}

					bool bDefaultModel = string.IsNullOrEmpty(Parameters.model);
					labelPromptTemplate.Enabled = CanEdit && !bDefaultModel;
					panelPromptTemplate.Enabled = CanEdit && !bDefaultModel;
				});
			}

			Dirty = true;
		}

		private void cbPromptTemplate_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int idxPromptTemplate = cbPromptTemplate.SelectedIndex;
			Parameters.iPromptTemplate = idxPromptTemplate;

			if (cbSavePromptTemplate.Checked)
				AppSettings.BackyardSettings.SetPromptTemplateForModel(Parameters.model, Parameters.iPromptTemplate);

			Dirty = true;
		}
		
		private void cbSavePromptTemplate_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			if (cbSavePromptTemplate.Checked)
				AppSettings.BackyardSettings.SetPromptTemplateForModel(Parameters.model, Parameters.iPromptTemplate);
			else
				AppSettings.BackyardSettings.SetPromptTemplateForModel(Parameters.model, -1);
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
			Dirty = true;

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
			Dirty = true;

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
			Dirty = true;

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
			Dirty = true;

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
			Dirty = true;

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
			Dirty = true;

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
			Dirty = true;
		}

		private void btnSavePreset_Click(object sender, EventArgs e)
		{
			int index = cbPresets.SelectedIndex;
			if (index < idxPresets)
				return;
			index -= idxPresets;
			if (index >= 0 && index < AppSettings.BackyardSettings.Presets.Count)
			{
				string presetName = AppSettings.BackyardSettings.Presets[index].name;
				AppSettings.BackyardSettings.Presets[index] = new AppSettings.BackyardSettings.Preset(presetName, Parameters);
			}
			Dirty = false;
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
				MsgBox.Error("A preset with this name already exists.", "Save preset", this);
				return;
			}

			AppSettings.BackyardSettings.Presets.Add(new AppSettings.BackyardSettings.Preset(presetName, Parameters));

			WhileIgnoringEvents(() => {
				cbPresets.BeginUpdate();
				cbPresets.Items.Add(presetName);
				cbPresets.SelectedIndex = cbPresets.Items.Count - 1;
				cbPresets.EndUpdate();
			});

			RefreshPresetButtons();
			RefreshValues();
			Dirty = false;
		}

		private void btnRemovePreset_Click(object sender, EventArgs e)
		{
			int index = cbPresets.SelectedIndex;
			if (index < idxPresets)
				return;

			if (MsgBox.Confirm("Remove this preset?", Resources.cap_confirm, this) == false)
				return;

			index -= idxPresets;
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
			Dirty = false;
		}

		private void RefreshPresetButtons()
		{
			btnNewPreset.Enabled = true;
			btnSavePreset.Enabled = CanSave;
			btnRemovePreset.Enabled = CanSave;

			btnCopy.Enabled = true;
			btnPaste.Enabled = CanEdit && Clipboard.ContainsData(ChatParametersClipboard.Format);
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			Parameters = new ChatParameters();
			RefreshValues();
			Dirty = true;
		}
	}
}
