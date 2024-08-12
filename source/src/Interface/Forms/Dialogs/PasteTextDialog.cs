using Ginger.Properties;
using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class PasteTextDialog : Form
	{
		public string RecipeXml = null;

		public PasteTextDialog()
		{
			InitializeComponent();

			comboBox.SelectedIndex = 1; // Character persona
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			switch (comboBox.SelectedIndex)
			{
			case 0: RecipeXml = Resources.system_recipe; break;
			default:
			case 1: RecipeXml = Resources.persona_recipe; break;
			case 2: RecipeXml = Resources.user_recipe; break;
			case 3: RecipeXml = Resources.scenario_recipe; break;
			case 4: RecipeXml = Resources.greeting_recipe; break;
			case 5: RecipeXml = Resources.example_recipe; break;
			case 6: RecipeXml = Resources.grammar_recipe; break;
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
