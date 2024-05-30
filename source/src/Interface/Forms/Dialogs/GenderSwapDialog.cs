using System;
using System.Windows.Forms;

namespace Ginger
{
	public partial class GenderSwapDialog : Form
	{
		public GenderSwap.Pronouns CharacterFrom;
		public GenderSwap.Pronouns CharacterTo = GenderSwap.Pronouns.VariableNeutral;
		public GenderSwap.Pronouns UserFrom;
		public GenderSwap.Pronouns UserTo = GenderSwap.Pronouns.VariableUserNeutral;
		public bool SwapCharacter = true;
		public bool SwapUser = false;
		public bool Valid = false;

		private bool _bIgnoreEvents = false;

		private GenderSwap.Pronouns[] CharacterFromList = new GenderSwap.Pronouns[]
		{
			GenderSwap.Pronouns.Masculine,
			GenderSwap.Pronouns.Feminine,
			GenderSwap.Pronouns.Neutral,
			GenderSwap.Pronouns.Mixed,
			GenderSwap.Pronouns.VariableNeutral,
			GenderSwap.Pronouns.VariableUserNeutral,
		};

		private GenderSwap.Pronouns[] CharacterToList = new GenderSwap.Pronouns[]
		{
			GenderSwap.Pronouns.Masculine,
			GenderSwap.Pronouns.Feminine,
			GenderSwap.Pronouns.Neutral,
			GenderSwap.Pronouns.Mixed,
			GenderSwap.Pronouns.VariableNeutral,
			GenderSwap.Pronouns.VariableUserNeutral,
		};

		private GenderSwap.Pronouns[] UserFromList = new GenderSwap.Pronouns[]
		{
			GenderSwap.Pronouns.Masculine,
			GenderSwap.Pronouns.Feminine,
			GenderSwap.Pronouns.Neutral,
			GenderSwap.Pronouns.Mixed,
			GenderSwap.Pronouns.VariableNeutral,
			GenderSwap.Pronouns.VariableUserNeutral,
		};

		private GenderSwap.Pronouns[] UserToList = new GenderSwap.Pronouns[]
		{
			GenderSwap.Pronouns.Masculine,
			GenderSwap.Pronouns.Feminine,
			GenderSwap.Pronouns.Neutral,
			GenderSwap.Pronouns.Mixed,
			GenderSwap.Pronouns.VariableNeutral,
			GenderSwap.Pronouns.VariableUserNeutral,
		};

		public GenderSwapDialog()
		{
			InitializeComponent();

			// Infer default values from current gender(s)
			CharacterFrom = GenderSwap.PronounsFromGender(Current.Character);
			UserFrom = GenderSwap.PronounsFromGender(Current.Card.userGender);

			Load += GenderSwapDialog_Load;
		}

		// Reduce flickering
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
				return cp;
			}
		}

		private void GenderSwapDialog_Load(object sender, EventArgs e)
		{
			_bIgnoreEvents = true;

			comboBox_CharacterGender.SelectedIndex = Array.IndexOf(CharacterFromList, CharacterFrom);
			comboBox_CharacterTarget.SelectedIndex = Array.IndexOf(CharacterToList, CharacterTo);
			comboBox_UserGender.SelectedIndex = Array.IndexOf(UserFromList, UserFrom);
			comboBox_UserTarget.SelectedIndex = Array.IndexOf(UserToList, UserTo);

			cbCharacter.Checked = SwapCharacter;
			comboBox_CharacterGender.Enabled = SwapCharacter;
			comboBox_CharacterTarget.Enabled = SwapCharacter;
			cbUser.Checked = SwapUser;
			comboBox_UserGender.Enabled = SwapUser;
			comboBox_UserTarget.Enabled = SwapUser;

			_bIgnoreEvents = false;

			ValidateValues();
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
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void comboBox_CharacterGender_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			CharacterFrom = CharacterFromList[comboBox_CharacterGender.SelectedIndex];
			ValidateValues();
		}

		private void comboBox_CharacterTarget_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			CharacterTo = CharacterToList[comboBox_CharacterTarget.SelectedIndex];
			ValidateValues();
		}

		private void comboBox_UserGender_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			UserFrom = UserFromList[comboBox_UserGender.SelectedIndex];
			ValidateValues();
		}

		private void comboBox_UserTarget_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			UserTo = UserToList[comboBox_UserTarget.SelectedIndex];
			ValidateValues();
		}

		private void cbCharacter_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			comboBox_CharacterGender.Enabled = cbCharacter.Checked;
			comboBox_CharacterTarget.Enabled = cbCharacter.Checked;
			ValidateValues();
		}

		private void cbUser_CheckedChanged(object sender, EventArgs e)
		{
			if (_bIgnoreEvents)
				return;

			comboBox_UserGender.Enabled = cbUser.Checked;
			comboBox_UserTarget.Enabled = cbUser.Checked;
			ValidateValues();
		}

		private void ValidateValues()
		{
			if (_bIgnoreEvents)
				return;

			SwapCharacter = cbCharacter.Checked;
			SwapUser = cbUser.Checked;
			bool bOk = SwapCharacter || SwapUser;

			if (SwapCharacter)
				bOk &= CharacterFrom != CharacterTo;
			if (SwapUser)
				bOk &= UserFrom != UserTo;
			if (SwapCharacter && SwapUser)
			{
				bOk &= CharacterFrom != UserFrom;
				bOk &= CharacterTo != UserTo;
			}

			btnOk.Enabled = bOk;
			Valid = bOk;
		}

	}
}
