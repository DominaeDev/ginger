using Ginger.Integration;
using Ginger.Properties;
using System;
using System.Globalization;
using System.Windows.Forms;

namespace Ginger
{
	public partial class EditModelSettingsDialog : FormEx
	{
		public bool EditingDefaults { get; set; }
		public ChatParameters Parameters { get; set; }

		private bool _bIgnoreEvents = false;

		private bool CanAssociate { get { return cbAssociate.Enabled && cbAssociate.Checked; } }

		public EditModelSettingsDialog()
		{
			InitializeComponent();

			Load += EditModelSettingsDialog_Load;

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
			SetToolTip(cbAssociate, Resources.tooltip_model_associate_prompt_template);
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
				cbPresets.Items.Add("Current settings");
				cbPresets.Items.Add("Backyard defaults");
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
			});

			if (EditingDefaults)
			{
				cbPresets.Enabled = false;
				btnNewPreset.Enabled = false;
				btnSavePreset.Enabled = false;
				btnRemovePreset.Enabled = false;
				btnCopy.Enabled = true;
				btnPaste.Enabled = CanPaste();
			}
			else
			{
				bool bCanEdit = cbPresets.SelectedIndex != 1;
				cbPresets.Enabled = true;
				btnNewPreset.Enabled = true;
				btnSavePreset.Enabled = bCanEdit;
				btnRemovePreset.Enabled = cbPresets.SelectedIndex >= 2;
				btnCopy.Enabled = true;
				btnPaste.Enabled = bCanEdit;
			}
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

					cbAssociate.Checked = false;
					cbAssociate.Enabled = false;
				}
				else
				{
					// Enable prompt template
					cbPromptTemplate.SelectedItem = cbPromptTemplate.Items[Parameters.iPromptTemplate];
					labelPromptTemplate.Enabled = true;
					panelPromptTemplate.Enabled = true;

#if false
					if (cbModel.SelectedIndex != 0)
					{
						cbAssociate.Checked = AppSettings.BackyardSettings.HasAssociatedModelSettings(Parameters.model);
						cbAssociate.Enabled = true;
					}
					else
					{
						cbAssociate.Checked = false;
						cbAssociate.Enabled = false;
					}
#endif
				}

				Set(textBox_Temperature, Parameters.temperature, "0.0", 0.1m);
				Set(textBox_MinP, Parameters.minP, "0.00", 0.05m);
				Set(textBox_TopP, Parameters.topP, "0.00", 0.05m);
				Set(textBox_TopK, Parameters.topK);
				Set(textBox_RepeatPenalty, Parameters.repeatPenalty, "0.00", 0.05m);
				Set(textBox_RepeatTokens, Parameters.repeatLastN);

				Set(trackBar_Temperature, Convert.ToInt32(Parameters.temperature * 10));
				Set(trackBar_MinP, Convert.ToInt32(Parameters.minP * 100));
				Set(trackBar_TopP, Convert.ToInt32(Parameters.topP * 100));
				Set(trackBar_TopK, Convert.ToInt32(Parameters.topK));
				Set(trackBar_RepeatPenalty, Convert.ToInt32(Parameters.repeatPenalty * 100));
				Set(trackBar_PenaltyTokens, Convert.ToInt32(Parameters.repeatLastN));

				if (Parameters.minPEnabled)
				{
					cbSampling.SelectedIndex = 0; // Min-P
					labelMinP.Enabled = true;
					panelMinP.Enabled = true;
					trackBar_MinP.Visible = true;
					labelTopP.Enabled = false;
					panelTopP.Enabled = false;
					trackBar_TopP.Visible = false;
					labelTopK.Enabled = false;
					panelTopK.Enabled = false;
					trackBar_TopK.Visible = false;
				}
				else
				{
					cbSampling.SelectedIndex = 1; // Top-K
					labelMinP.Enabled = false;
					panelMinP.Enabled = false;
					trackBar_MinP.Visible = false;
					labelTopP.Enabled = true;
					panelTopP.Enabled = true;
					trackBar_TopP.Visible = true;
					labelTopK.Enabled = true;
					panelTopK.Enabled = true;
					trackBar_TopK.Visible = true;
				}
			});
		}

		private void Set(TextBoxBase textBox, decimal value, string format = "0", decimal partition = 0.0m)
		{
			value = Utility.RoundNearest(value, partition);
			textBox.Text = value.ToString(format, CultureInfo.InvariantCulture);
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
			SaveAssociatedSettings();

			bool bMinPEnabled = cbSampling.SelectedIndex == 0;
			labelMinP.Enabled = bMinPEnabled;
			panelMinP.Enabled = bMinPEnabled;
			trackBar_MinP.Visible = bMinPEnabled;
			labelTopP.Enabled = !bMinPEnabled;
			panelTopP.Enabled = !bMinPEnabled;
			trackBar_TopP.Visible = !bMinPEnabled;
			labelTopK.Enabled = !bMinPEnabled;
			panelTopK.Enabled = !bMinPEnabled;
			trackBar_TopK.Visible = !bMinPEnabled;
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
			bool bCanEdit = index != 1;
			bool bPreset = index > 1;

			if (index == 0)
				Parameters = AppSettings.BackyardSettings.UserSettings.Copy();
			else if (index == 1)
				Parameters = ChatParameters.Default;
			else if (index >= 2 && index < AppSettings.BackyardSettings.Presets.Count + 2)
				Parameters = AppSettings.BackyardSettings.Presets[index - 2].Copy();

#if false
			if (bCanEdit && string.IsNullOrEmpty(Parameters.model) == false)
			{
				// Per-model settings override
				cbAssociate.Enabled = true;
				var associatedSettings = AppSettings.BackyardSettings.GetAssociatedModelSettings(Parameters.model);
				if (associatedSettings != null)
					Parameters = associatedSettings;
			}
#endif

			bool bMinPEnabled = Parameters.minPEnabled;
			bool bDefaultModel = string.IsNullOrEmpty(Parameters.model);

			RefreshValues();

			btnNewPreset.Enabled = bCanEdit && !EditingDefaults;
			btnSavePreset.Enabled = bCanEdit && !EditingDefaults;
			btnRemovePreset.Enabled = bPreset;
			btnCopy.Enabled = true;
			btnPaste.Enabled = bCanEdit && CanPaste();

			// Enable all
			labelModel.Enabled = bCanEdit;
			labelPromptTemplate.Enabled = bCanEdit && !bDefaultModel;
			labelTemperature.Enabled = bCanEdit;
			labelSampling.Enabled = bCanEdit;
			labelMinP.Enabled = bCanEdit && bMinPEnabled;
			labelTopP.Enabled = bCanEdit && !bMinPEnabled;
			labelTopK.Enabled = bCanEdit && !bMinPEnabled;
			labelRepeatPenalty.Enabled = bCanEdit;
			labelPenaltyTokens.Enabled = bCanEdit;
			panelModel.Enabled = bCanEdit;
			panelPromptTemplate.Enabled = bCanEdit && !bDefaultModel;
			panelTemperature.Enabled = bCanEdit;
			panelSampling.Enabled = bCanEdit;
			panelMinP.Enabled = bCanEdit && bMinPEnabled;
			panelTopP.Enabled = bCanEdit && !bMinPEnabled;
			panelTopK.Enabled = bCanEdit && !bMinPEnabled;
			panelRepeatPenalty.Enabled = bCanEdit;
			panelPenaltyTokens.Enabled = bCanEdit;
			trackBar_Temperature.Visible = bCanEdit;
			trackBar_MinP.Visible = bCanEdit && bMinPEnabled;
			trackBar_TopP.Visible = bCanEdit && !bMinPEnabled;
			trackBar_TopK.Visible = bCanEdit && !bMinPEnabled;
			trackBar_RepeatPenalty.Visible = bCanEdit;
			trackBar_PenaltyTokens.Visible = bCanEdit;
		}

		private void cbModel_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int index = cbModel.SelectedIndex;
			if (index == 0)
			{
				Parameters.model = null;

				// Disable prompt template
				WhileIgnoringEvents(() => {
					// Disable prompt template
					cbPromptTemplate.SelectedIndex = 0;
					labelPromptTemplate.Enabled = false;
					panelPromptTemplate.Enabled = false;

					cbAssociate.Enabled = false;
					cbAssociate.Checked = true;
				});
			}
			else
			{
				Parameters.model = cbModel.SelectedItem as string;

				WhileIgnoringEvents(() => {
					// Enable prompt template
					labelPromptTemplate.Enabled = true;
					panelPromptTemplate.Enabled = true;

#if false
					// Associated?
					cbAssociate.Enabled = true;
					cbAssociate.Checked = AppSettings.BackyardSettings.HasAssociatedModelSettings(Parameters.model);
#endif
				});
			}

#if false
			if (CanAssociate && Parameters.model != null) // Fetch per-model settings
			{
				var associatedSettings = AppSettings.BackyardSettings.GetAssociatedModelSettings(Parameters.model);
				if (associatedSettings != null)
				{
					Parameters = associatedSettings;
					RefreshValues();
					return;
				}
			}
			else
#endif
			{
				// Reset prompt settings
				WhileIgnoringEvents(() => {
					Parameters.iPromptTemplate = 0;
					cbPromptTemplate.SelectedIndex = 0;
				});
			}
		}

		private void cbPromptTemplate_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			int idxPromptTemplate = cbPromptTemplate.SelectedIndex;
			Parameters.iPromptTemplate = idxPromptTemplate;

			SaveAssociatedSettings();
		}

		private void SaveAssociatedSettings()
		{
#if false
			if (CanAssociate && Parameters.model != null)
				AppSettings.BackyardSettings.AssociateModelSettings(Parameters.model, Parameters);
#endif
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
			SaveAssociatedSettings();

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
			SaveAssociatedSettings();

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
			SaveAssociatedSettings();

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
			SaveAssociatedSettings();

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
			SaveAssociatedSettings();

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
			SaveAssociatedSettings();

			WhileIgnoringEvents(() => {
				Set(trackBar_PenaltyTokens, value);
			});
		}

		private void cbAssociate_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

#if false
			if (cbAssociate.Checked)
				SaveAssociatedSettings();
			else if (Parameters.model != null)
				AppSettings.BackyardSettings.AssociateModelSettings(Parameters.model, null);
#endif
		}

		private void TextBox_Temperature_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			WhileIgnoringEvents(() => {
				textBox_Temperature.Text = Parameters.temperature.ToString("0.0", CultureInfo.InvariantCulture);
			});
		}

		private void TextBox_MinP_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			WhileIgnoringEvents(() => {
				textBox_MinP.Text = Parameters.minP.ToString("0.0#", CultureInfo.InvariantCulture);
			});
		}

		private void TextBox_TopP_LostFocus(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			WhileIgnoringEvents(() => {
				textBox_TopP.Text = Parameters.topP.ToString("0.0#", CultureInfo.InvariantCulture);
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
				textBox_RepeatPenalty.Text = Parameters.repeatPenalty.ToString("0.0#", CultureInfo.InvariantCulture);
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

		private bool CanPaste()
		{
			return Clipboard.ContainsData(ChatParametersClipboard.Format);
		}

		private void btnCopy_Click(object sender, EventArgs e)
		{
			Clipboard.SetDataObject(ChatParametersClipboard.FromParameters(Parameters), false);
			btnPaste.Enabled = true;
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
			if (index == 0) // User defaults
			{
				AppSettings.BackyardSettings.UserSettings = Parameters.Copy();
				return;
			}
			else if (index == 1) // Backyard defaults
			{
				return;
			}
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

			Parameters = AppSettings.BackyardSettings.UserSettings.Copy();
			RefreshValues();
		}
	}
}
