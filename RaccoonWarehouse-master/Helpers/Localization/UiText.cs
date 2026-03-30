using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;

namespace RaccoonWarehouse.Helpers.Localization
{
    public static class UiText
    {
        private static readonly Dictionary<string, string> TranslationCache = new(StringComparer.Ordinal);
        private static Dictionary<string, string>? _literalTranslations;

        public static bool IsEnglish => System.Windows.Application.Current is App app && app.IsEnglish;

        public static string T(string arabic, string english) => IsEnglish ? english : arabic;

        public static FlowDirection CurrentFlowDirection => IsEnglish ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;

        public static void ResetCache()
        {
            TranslationCache.Clear();
            _literalTranslations = null;
        }

        public static string Translate(string text)
        {
            if (!IsEnglish || string.IsNullOrWhiteSpace(text))
                return text;

            if (TranslationCache.TryGetValue(text, out var cached))
                return cached;

            var translated = TryTranslateExact(text)
                ?? TryTranslateTrimmed(text)
                ?? text;

            TranslationCache[text] = translated;
            return translated;
        }

        public static void ApplyWindow(Window window)
        {
            window.FlowDirection = CurrentFlowDirection;
            window.Language = XmlLanguage.GetLanguage(IsEnglish ? "en-US" : "ar-JO");
            window.Title = Translate(window.Title);
            ApplyTranslations(window);
        }

        public static void ApplyTranslations(object root)
        {
            if (!IsEnglish || root == null)
                return;

            TranslateObject(root, new HashSet<int>());
        }

        public static void ApplyDocument(FlowDocument document)
        {
            if (document == null)
                return;

            document.FlowDirection = CurrentFlowDirection;
            ApplyTranslations(document);
        }

        private static void TranslateObject(object node, HashSet<int> visited)
        {
            var nodeId = RuntimeHelpers.GetHashCode(node);
            if (!visited.Add(nodeId))
                return;

            switch (node)
            {
                case Window window:
                    window.Title = Translate(window.Title);
                    break;
                case TextBlock textBlock:
                    textBlock.Text = Translate(textBlock.Text);
                    break;
                case AccessText accessText:
                    accessText.Text = Translate(accessText.Text);
                    break;
                case Label label:
                    label.Content = TranslateObjectValue(label.Content);
                    label.ToolTip = TranslateObjectValue(label.ToolTip);
                    break;
                case MenuItem menuItem:
                    menuItem.Header = TranslateObjectValue(menuItem.Header);
                    menuItem.ToolTip = TranslateObjectValue(menuItem.ToolTip);
                    break;
                case HeaderedItemsControl headeredItemsControl when node is Expander expander:
                    expander.Header = TranslateObjectValue(expander.Header);
                    expander.ToolTip = TranslateObjectValue(expander.ToolTip);
                    break;
                case Run run:
                    run.Text = Translate(run.Text);
                    break;
                case TextBox textBox:
                    textBox.Text = Translate(textBox.Text);
                    textBox.ToolTip = TranslateObjectValue(textBox.ToolTip);
                    break;
                case PasswordBox passwordBox:
                    passwordBox.ToolTip = TranslateObjectValue(passwordBox.ToolTip);
                    break;
                case ComboBox comboBox:
                    comboBox.ToolTip = TranslateObjectValue(comboBox.ToolTip);
                    TranslateComboBox(comboBox);
                    break;
                case ComboBoxItem comboBoxItem:
                    comboBoxItem.Content = TranslateObjectValue(comboBoxItem.Content);
                    comboBoxItem.ToolTip = TranslateObjectValue(comboBoxItem.ToolTip);
                    break;
                case Selector selector:
                    selector.ToolTip = TranslateObjectValue(selector.ToolTip);
                    break;
                case HeaderedContentControl headeredContentControl:
                    headeredContentControl.Header = TranslateObjectValue(headeredContentControl.Header);
                    headeredContentControl.Content = TranslateObjectValue(headeredContentControl.Content);
                    headeredContentControl.ToolTip = TranslateObjectValue(headeredContentControl.ToolTip);
                    break;
                case HeaderedItemsControl headeredItemsControl:
                    headeredItemsControl.Header = TranslateObjectValue(headeredItemsControl.Header);
                    headeredItemsControl.ToolTip = TranslateObjectValue(headeredItemsControl.ToolTip);
                    break;
                case ContentControl contentControl:
                    contentControl.Content = TranslateObjectValue(contentControl.Content);
                    contentControl.ToolTip = TranslateObjectValue(contentControl.ToolTip);
                    break;
                case FrameworkElement frameworkElement:
                    frameworkElement.ToolTip = TranslateObjectValue(frameworkElement.ToolTip);
                    break;
            }

            if (node is DataGrid dataGrid)
            {
                foreach (var column in dataGrid.Columns)
                {
                    column.Header = TranslateObjectValue(column.Header);
                }
            }

            if (node is ItemsControl itemsControl)
            {
                foreach (var item in itemsControl.Items)
                    TranslateObject(item, visited);
            }

            if (node is DependencyObject dependencyObject)
            {
                foreach (var child in LogicalTreeHelper.GetChildren(dependencyObject))
                    TranslateObject(child, visited);
            }

            if (node is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                        TranslateObject(item, visited);
                }
            }
        }

        private static object? TranslateObjectValue(object? value)
        {
            return value switch
            {
                string text => Translate(text),
                _ => value
            };
        }

        private static void TranslateComboBox(ComboBox comboBox)
        {
            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem)
                {
                    comboBoxItem.Content = TranslateObjectValue(comboBoxItem.Content);
                    comboBoxItem.ToolTip = TranslateObjectValue(comboBoxItem.ToolTip);
                }
            }

            if (comboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content is string selectedText)
            {
                comboBox.Text = Translate(selectedText);
                return;
            }

            if (!string.IsNullOrWhiteSpace(comboBox.Text))
                comboBox.Text = Translate(comboBox.Text);
        }

        private static string? TryTranslateExact(string text)
        {
            var translations = GetTranslations();
            return translations.TryGetValue(text, out var translated) ? translated : null;
        }

        private static string? TryTranslateTrimmed(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.Length == 0 || trimmed.Length == text.Length)
                return null;

            var translated = TryTranslateExact(trimmed);
            if (translated == null)
                return null;

            var leading = text.Length - text.TrimStart().Length;
            var trailing = text.Length - text.TrimEnd().Length;
            return string.Concat(new string(' ', leading), translated, new string(' ', trailing));
        }

        private static Dictionary<string, string> GetTranslations()
        {
            if (_literalTranslations != null)
                return _literalTranslations;

            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Localization", "LiteralStrings.en.json");
                if (!File.Exists(path))
                    return _literalTranslations = new Dictionary<string, string>(StringComparer.Ordinal);

                var json = File.ReadAllText(path);
                _literalTranslations = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                    ?? new Dictionary<string, string>(StringComparer.Ordinal);
            }
            catch
            {
                _literalTranslations = new Dictionary<string, string>(StringComparer.Ordinal);
            }

            return _literalTranslations;
        }
    }
}
