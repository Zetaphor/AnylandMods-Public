using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods
{
    public abstract class MenuItem {
        public string Id { get; private set; }
        public string Text { get; set; }
        public delegate void ItemAction(string id, Dialog dialog);
        public event ItemAction Action;

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
            return dialog.AddButton(Id, null, Text, "ButtonCompact", xOnFundament, yOnFundament, Icon, textColor: TextColor, align: TextAlignment.Center);
        }
    }

    public class MenuCheckbox : MenuDataItem<bool> {
        public ExtraIcon ExtraIcon { get; set; } = ExtraIcon.None;
        public string Footnote { get; set; } = "";
        public TextColor TextColor { get; set; } = TextColor.Default;

        public MenuCheckbox(string id, string text) : base(id, text) { }

        public override GameObject Create(Dialog dialog, int xOnFundament, int yOnFundament)
        {
            return dialog.AddCheckbox(Id, null, Text, xOnFundament, yOnFundament, Value, textColor: TextColor, footnote: Footnote, extraIcon: ExtraIcon);
        }
    }
}
