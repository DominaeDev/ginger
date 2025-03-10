using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ginger
{
	public class ActorChoiceParameterPanel : ChoiceParameterPanel
	{
		protected override List<ChoiceParameter.Item> ChoiceItems
		{
			get
			{
				var items = new List<ChoiceParameter.Item>(Current.Characters.Count);
				for (int i = 0; i < Current.Characters.Count; ++i)
				{
					items.Add(new ChoiceParameter.Item() {
						id = string.Format("actor-{0}", i),
						label = Current.Characters[i].spokenName,
						value = i.ToString(),
					});
				}
				return items;
			}
		}

		protected override void OnRefreshValue()
		{
			// Refresh list
			comboBox.BeginUpdate();
			comboBox.Items.Clear();
			// Drop down
			if (parameter.isOptional)
				comboBox.Items.Add("\u2014"); // Empty
			foreach (var item in ChoiceItems)
				comboBox.Items.Add(item.label);
			comboBox.EndUpdate();

			base.OnRefreshValue();
		}
	}
}
