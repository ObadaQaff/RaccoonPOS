using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.Helpers
{
    /// <summary>Reusable supplier selector behavior. Typing filters locally and never starts a report query.</summary>
    public static class SupplierComboBoxSearch
    {
        private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
            "State", typeof(State), typeof(SupplierComboBoxSearch), new PropertyMetadata(null));

        public static void Attach(ComboBox comboBox)
        {
            if (comboBox == null || comboBox.GetValue(StateProperty) != null) return;
            var state = new State(comboBox);
            comboBox.SetValue(StateProperty, state);
        }

        public static void Refresh(ComboBox comboBox)
        {
            if (comboBox?.GetValue(StateProperty) is State state)
                state.CaptureItems();
        }

        public static void SetItems(ComboBox comboBox, IEnumerable<UserReadDto> items)
        {
            if (comboBox?.GetValue(StateProperty) is State state)
                state.SetItems(items);
            else if (comboBox != null)
                comboBox.ItemsSource = items.ToList();
        }

        private sealed class State
        {
            private readonly ComboBox _combo;
            private readonly List<UserReadDto> _all = new();
            private bool _updating;
            private TextBox? _editableTextBox;
            private string _search = string.Empty;

            public State(ComboBox combo)
            {
                _combo = combo;
                combo.Loaded += (_, _) => CaptureItems();
                combo.PreviewKeyDown += KeyDown;

                DependencyPropertyDescriptor
                    .FromProperty(ItemsControl.ItemsSourceProperty, typeof(ComboBox))?
                    .AddValueChanged(combo, ItemsSourceChanged);
            }

            private void ItemsSourceChanged(object? sender, EventArgs e)
            {
                if (!_updating)
                    CaptureItems();
            }

            internal void CaptureItems()
            {
                _all.Clear();
                if (_combo.ItemsSource is IEnumerable<UserReadDto> users)
                    _all.AddRange(users.Where(x => x.Id != 0));
                if (_combo.Template.FindName("PART_EditableTextBox", _combo) is TextBox textBox
                    && !ReferenceEquals(_editableTextBox, textBox))
                {
                    if (_editableTextBox != null)
                        _editableTextBox.TextChanged -= TextChanged;

                    _editableTextBox = textBox;
                    textBox.TextChanged += TextChanged;
                }
            }

            internal void SetItems(IEnumerable<UserReadDto> items)
            {
                var source = items.ToList();
                _all.Clear();
                _all.AddRange(source.Where(x => x.Id != 0));

                _updating = true;
                try
                {
                    _combo.ItemsSource = source;
                }
                finally
                {
                    _updating = false;
                }

                CaptureItems();
            }

            private void TextChanged(object sender, TextChangedEventArgs e)
            {
                if (_updating || sender is not TextBox textBox) return;

                // WPF can raise TextChanged when SelectedIndex is initialized,
                // before the report has explicitly refreshed the helper cache.
                // Capture the original supplier source before filtering it away.
                if (_all.Count == 0)
                    CaptureItems();

                var allSuppliersSelected = _combo.SelectedItem is UserReadDto { Id: 0 };
                _search = allSuppliersSelected ? string.Empty : textBox.Text.Trim();
                _updating = true;
                try
                {
                    var filtered = string.IsNullOrWhiteSpace(_search)
                        ? _all.ToList()
                        : _all.Where(x => (x.Name ?? string.Empty).Contains(_search, StringComparison.CurrentCultureIgnoreCase)
                                       || (x.PhoneNumber ?? string.Empty).Contains(_search, StringComparison.CurrentCultureIgnoreCase)).ToList();
                    if (string.IsNullOrWhiteSpace(_search))
                        filtered.Insert(0, new UserReadDto { Id = 0, Name = "All suppliers" });

                    _combo.SelectedValue = string.IsNullOrWhiteSpace(_search) ? 0 : null;
                    _combo.ItemsSource = filtered;
                    textBox.Text = _search;
                    textBox.CaretIndex = textBox.Text.Length;
                    _combo.IsDropDownOpen = true;
                }
                finally { _updating = false; }
            }

            private void KeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key is not (Key.Down or Key.Up or Key.Enter) || _combo.Items.Count == 0) return;
                _updating = true;
                try
                {
                    if (e.Key == Key.Down) _combo.SelectedIndex = Math.Min(_combo.SelectedIndex + 1, _combo.Items.Count - 1);
                    else if (e.Key == Key.Up) _combo.SelectedIndex = Math.Max(_combo.SelectedIndex - 1, 0);
                    else { _combo.IsDropDownOpen = false; e.Handled = true; return; }
                    if (_combo.Template.FindName("PART_EditableTextBox", _combo) is TextBox textBox)
                    {
                        textBox.Text = _search;
                        textBox.CaretIndex = textBox.Text.Length;
                    }
                    _combo.IsDropDownOpen = true;
                    e.Handled = true;
                }
                finally { _updating = false; }
            }
        }
    }
}
