using Ginger.Properties;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ginger
{
	public partial class CreateRecipeDialog : FormEx
	{
		public string RecipeName;
		public string RecipeTitle;
		public string FileName;
		public string RecipeXml;
		public Recipe.Category Category = Recipe.Category.Undefined;
		public bool FromRecipe = false;

		private Recipe.Category[] Categories = new Recipe.Category[] {
			Recipe.Category.Base,
			Recipe.Category.Model,
			Recipe.Category.Modifier,
			
			Recipe.Category.Character,
			Recipe.Category.Trait,
			Recipe.Category.Special,
			Recipe.Category.Mind,
			Recipe.Category.Story,

			Recipe.Category.Archetype,
			Recipe.Category.Appearance,
			Recipe.Category.Body,
			Recipe.Category.Feature,
			Recipe.Category.Speech,

			Recipe.Category.Personality,
			Recipe.Category.Behavior,
			Recipe.Category.Quirk,
			Recipe.Category.Emotion,
			Recipe.Category.Sexual,

			Recipe.Category.User,
			Recipe.Category.Relationship,

			Recipe.Category.Scenario,
			Recipe.Category.World,
			Recipe.Category.Location,
			Recipe.Category.Role,
			Recipe.Category.Job,
			Recipe.Category.Cast,
			Recipe.Category.Concept,

			Recipe.Category.Custom,

//			Recipe.Category.Lore,
//			Recipe.Category.Dialogue,
//			Recipe.Category.Grammar,
		};

		public CreateRecipeDialog()
		{
			InitializeComponent();

			Shown += CreateRecipeDialog_Shown;
			Load += CreateRecipeDialog_Load;

			textBox_Name.EnterPressed += BtnOk_Click;
						
			// Category
			comboBox_Category.Items.AddRange(Categories.Select(e => EnumHelper.ToString(e)).ToArray());
			comboBox_Template.SelectedIndexChanged += ComboBox_Template_SelectedIndexChanged;

			// Launch text editor
			checkBoxOpenTextEditor.Checked = AppSettings.User.LaunchTextEditor;
		}

		private void ComboBox_Template_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (FromRecipe == false)
			{
				if (comboBox_Template.SelectedIndex == 2) // Personality
					comboBox_Category.SelectedIndex = Array.IndexOf(Categories, Recipe.Category.Personality);
				else if (comboBox_Template.SelectedIndex == 3) // Base
					comboBox_Category.SelectedIndex = Array.IndexOf(Categories, Recipe.Category.Base);
			}
		}

		private void CreateRecipeDialog_Load(object sender, EventArgs e)
		{
			// Template
			if (FromRecipe)
			{
				comboBox_Template.Items.AddRange(new string[] {
					"From recipe"});
				comboBox_Template.Enabled = false;
				comboBox_Template.SelectedIndex = 0; // Empty
			}
			else
			{
				comboBox_Template.Items.AddRange(new string[] {
					"Empty",
					"From current character",
					"Personality trait",
					"Base recipe",
					"(Sample) Trait recipe ",
					"(Sample) Recipe with parameters",
					"(Sample) Recipe with lore"});
				comboBox_Template.Enabled = true;
				comboBox_Template.SelectedIndex = 0; // Empty
			}

		}

		private void CreateRecipeDialog_Shown(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(RecipeName) == false)
				textBox_Name.Text = RecipeName;
			else
				textBox_Name.Text = "New recipe";
			textBox_Name.SelectAll();

			textBox_Name.Focus();

			if (Category != Recipe.Category.Undefined)
				comboBox_Category.SelectedIndex = Array.IndexOf(Categories, Category);
			else
				comboBox_Category.SelectedIndex = Array.IndexOf(Categories, Recipe.Category.Custom);

		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == ShortcutKeys.Cancel)
			{
				DialogResult = DialogResult.Cancel;
				Close();
				return true;
			}
			return false;
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			// Ensure name
			RecipeName = textBox_Name.Text.Trim();
			if (string.IsNullOrEmpty(RecipeName))
				return;

			// Path
			var lsPath = RecipeName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToList();

			string filename = Utility.ValidFilename(lsPath[lsPath.Count - 1]);
			StringBuilder sbFullName = new StringBuilder();
			foreach (var path in lsPath)
			{
				if (sbFullName.Length > 0)
					sbFullName.Append('/');
				sbFullName.Append(path);
			}
			RecipeName = sbFullName.ToString();
			RecipeTitle = lsPath[lsPath.Count - 1];

			// Prepare texts
			if (FromRecipe)
			{
				RecipeXml = Resources.recipe_template_card;
			}
			else
			{
				switch (comboBox_Template.SelectedIndex)
				{
				default:
				case 0: // Empty
					RecipeXml = Resources.recipe_template_empty;
					break;
				case 1: // From recipe
					RecipeXml = Resources.recipe_template_card;
					break;
				case 2: // Personality
					RecipeXml = Resources.recipe_template_personality;
					break;
				case 3: // Base recipe
					RecipeXml = Resources.recipe_template_base;
					break;
				case 4: // Sample: Trait
					RecipeXml = Resources.recipe_template_sample_1;
					break;
				case 5: // Sample: Parameters
					RecipeXml = Resources.recipe_template_sample_2;
					break;
				case 6: // Sample: Lore
					RecipeXml = Resources.recipe_template_sample_3;
					break;
				}
			}

			if (filename.Length == 0)
				return;

			FileName = Utility.ContentPath("Recipes\\User", string.Concat(filename.ToLowerInvariant(), ".recipe.xml"));

			if (File.Exists(FileName) 
				&& MsgBox.Confirm(Resources.msg_overwrite_recipe, Resources.cap_overwrite_recipe, this) == false)
				return;

			Category = Categories[comboBox_Category.SelectedIndex];

			AppSettings.User.LaunchTextEditor = checkBoxOpenTextEditor.Checked;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			RecipeName = null;
			FileName = null;
			RecipeXml = null;
			Category = Recipe.Category.Undefined;
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void TextBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyData == Keys.Return || e.KeyData == Keys.Enter)
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		

	}
}
