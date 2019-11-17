using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods
{
    public abstract class MenuItem : IComparable<MenuItem> {
        public string Id { get; private set; }
        public string Text { get; set; }
        public GameObject GameObject { get; protected set; }
        public delegate void ItemAction(string id, Dialog dialog);
        public event ItemAction Action;
        public int SortWeight { get; set; } = 0;

        public MenuItem(string id, string text)
        {
            Id = id;
            Text = text;
        }

        public virtual void OnAction(Dialog dialog)
        {
            if (Action != null)
                Action(Id, dialog);
        }

        public abstract GameObject Create(Dialog dialog, int xOnFundament, int yOnFundament);
        
        public override bool Equals(object obj) => obj is MenuItem mi && Id.Equals(mi.Id);
        public int CompareTo(MenuItem other) => SortWeight == other.SortWeight ? Id.CompareTo(other.Id) : Math.Sign(SortWeight - other.SortWeight);
        public override int GetHashCode() => Id.GetHashCode();
    }

    public abstract class MenuDataItem<TValue> : MenuItem {
        public TValue Value { get; set; }
        public delegate void DataItemAction(string id, Dialog dialog, TValue value);
        public new event DataItemAction Action;

        public MenuDataItem(string id, string text, TValue value = default) : base(id, text)
        {
            Value = value;
        }

        public override void OnAction(Dialog dialog)
        {
            base.OnAction(dialog);
            OnAction(dialog, Value);
        }

        public virtual void OnAction(Dialog dialog, TValue value)
        {
            if (Action != null)
                Action(Id, dialog, value);
        }
    }

    public class MenuButton : MenuItem {
        public string Icon { get; set; } = null;
        public TextColor TextColor { get; set; } = TextColor.Default;

        public MenuButton(string id, string text) : base(id, text) { }

        public override GameObject Create(Dialog dialog, int xOnFundament, int yOnFundament)
        {
            return GameObject = dialog.AddButton(Id, null, Text, "ButtonCompact", xOnFundament, yOnFundament, Icon, textColor: TextColor, align: TextAlignment.Center);
        }
    }

    public class MenuCheckbox : MenuDataItem<bool> {
        public ExtraIcon ExtraIcon { get; set; } = ExtraIcon.None;
        public string Footnote { get; set; } = "";
        public TextColor TextColor { get; set; } = TextColor.Default;

        public MenuCheckbox(string id, string text) : base(id, text) { }

        public override GameObject Create(Dialog dialog, int xOnFundament, int yOnFundament)
        {
            return GameObject = dialog.AddCheckbox(Id, null, Text, xOnFundament, yOnFundament, Value, textColor: TextColor, footnote: Footnote, extraIcon: ExtraIcon);
        }
    }

    public class MenuSlider : MenuDataItem<float> {
        public string LabelPrefix {
            get => Text;
            set => Text = value;
        }
        public string LabelSuffix { get; set; } = "";
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public bool RoundValues { get; set; } = false;
        public bool ShowValue { get; set; } = true;

        private Dialog dialog;
        
        public MenuSlider(string labelPrefix, float minValue, float value, float maxValue, string labelSuffix = "")
            : base("", labelPrefix)
        {
            MinValue = minValue;
            Value = value;
            MaxValue = maxValue;
            LabelSuffix = labelSuffix;
        }

        public MenuSlider(string labelPrefix, float minValue, float maxValue, string labelSuffix = "")
            : this(labelPrefix, minValue, minValue, maxValue, labelSuffix)
        {
        }

        public override GameObject Create(Dialog dialog, int xOnFundament, int yOnFundament)
        {
            this.dialog = dialog;
            GameObject x = dialog.AddSlider(LabelPrefix, LabelSuffix, xOnFundament, yOnFundament, MinValue, MaxValue, RoundValues, Value, SliderAction, ShowValue).gameObject;
            return GameObject = x;
        }

        private void SliderAction(float value)
        {
            Value = value;
            OnAction(dialog, value);
        }
    }
}
