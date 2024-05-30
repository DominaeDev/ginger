namespace Ginger
{
	// These dummies only exist to placate the WinForms designer in VS2019.
	// The forms designer cannot handle generics properly and this is the workaround.

	public class TextParameterPanelDummy : TextParameterPanelBase { }
	public class MultiTextParameterPanelDummy : TextParameterPanelBase { }
	public class ChatParameterPanelDummy : TextParameterPanelBase { }
	public class CodeParameterPanelDummy : TextParameterPanelBase { }
	public class ToggleParameterPanelDummy : ParameterPanel<BooleanParameter> {}
	public class ListParameterPanelDummy : ParameterPanel<ListParameter>{}
	public class NumberParameterPanelDummy : ParameterPanel<NumberParameter> { }
	public class SliderParameterPanelDummy : ParameterPanel<RangeParameter> { }
	public class ChoiceParameterPanelDummy : ParameterPanel<ChoiceParameter> { }
	public class ChoiceSliderParameterPanelDummy : ParameterPanel<ChoiceParameter> { }
	public class RadioParameterPanelDummy : ParameterPanel<ChoiceParameter> { }
	public class HintParameterPanelDummy : ParameterPanel<HintParameter> { }
	public class LoreBookParameterPanelDummy : ParameterPanel<LorebookParameter> { }
	public class MeasurementParameterPanelDummy : ParameterPanel<MeasurementParameter> { }
	public class MultiChoiceParameterPanelDummy : ParameterPanel<MultiChoiceParameter> { }
}
