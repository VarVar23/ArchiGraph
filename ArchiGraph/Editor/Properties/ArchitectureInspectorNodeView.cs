using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace ArchiGraph
{
    public class ArchitectureInspectorNodeView : Node
    {
        public Action<bool> OnApply;

        private Label _namespace;
        private VisualElement _depsContainer;

        private ColorField _classColorField;
        private ColorField _interfaceColorField;
        private ColorField _inputColorField;
        private FloatField _offsetField;
        private Toggle _showDepsToggle;

        private Color _tmpClassColor;
        private Color _tmpInterfaceColor;
        private Color _tmpInputColor;
        private float _tmpOffset;
        private bool _tmpShowDeps;

        private const float FIELD_WIDTH = 240f;

        public ArchitectureInspectorNodeView()
        {
            title = "Properties";
            capabilities |= Capabilities.Movable | Capabilities.Resizable;

            style.paddingLeft = 6;
            style.paddingRight = 6;

            _tmpClassColor = ArchitectureGraphSettings.ClassColor;
            _tmpInterfaceColor = ArchitectureGraphSettings.InterfaceColor;
            _tmpInputColor = ArchitectureGraphSettings.InputColor;
            _tmpOffset = ArchitectureGraphSettings.Offset;
            _tmpShowDeps = ArchitectureGraphSettings.ShowDependency;

            _namespace = new Label();
            mainContainer.Add(_namespace);

            _depsContainer = new VisualElement();
            _depsContainer.style.marginBottom = 10;
            mainContainer.Add(_depsContainer);

            var header = new Label("Colors");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 6;
            mainContainer.Add(header);

            _classColorField = CreateColor("Output Class Color", _tmpClassColor, c => _tmpClassColor = c);
            mainContainer.Add(_classColorField);

            _interfaceColorField = CreateColor("Output Interface Color", _tmpInterfaceColor, c => _tmpInterfaceColor = c);
            mainContainer.Add(_interfaceColorField);

            _inputColorField = CreateColor("Input Color", _tmpInputColor, c => _tmpInputColor = c);
            mainContainer.Add(_inputColorField);

            var spacingHeader = new Label("Layout");
            spacingHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            spacingHeader.style.marginTop = 10;
            spacingHeader.style.marginBottom = 6;
            mainContainer.Add(spacingHeader);

            _offsetField = new FloatField("Offset");
            _offsetField.style.width = FIELD_WIDTH;
            _offsetField.value = _tmpOffset;
            _offsetField.RegisterValueChangedCallback(evt => _tmpOffset = evt.newValue);
            mainContainer.Add(_offsetField);

            _showDepsToggle = new Toggle("Show Dependency");
            _showDepsToggle.value = _tmpShowDeps;
            _showDepsToggle.RegisterValueChangedCallback(evt => _tmpShowDeps = evt.newValue);
            mainContainer.Add(_showDepsToggle);

            var apply = new Button(() =>
            {
                float oldOffset = ArchitectureGraphSettings.Offset;
                bool offsetChanged = !Mathf.Approximately(oldOffset, _tmpOffset);

                ArchitectureGraphSettings.ClassColor = _tmpClassColor;
                ArchitectureGraphSettings.InterfaceColor = _tmpInterfaceColor;
                ArchitectureGraphSettings.InputColor = _tmpInputColor;

                ArchitectureGraphSettings.Offset = _tmpOffset;
                ArchitectureGraphSettings.ShowDependency = _tmpShowDeps;

                OnApply?.Invoke(offsetChanged);
            });

            apply.text = "Apply";
            apply.style.width = FIELD_WIDTH;
            apply.style.marginTop = 8;
            mainContainer.Add(apply);

            RefreshExpandedState();
            RefreshPorts();
        }

        private ColorField CreateColor(string label, Color value, Action<Color> onChanged)
        {
            var f = new ColorField(label);
            f.value = value;
            f.style.width = FIELD_WIDTH;
            f.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            return f;
        }

        public void ShowNode(Type type, List<Type> deps)
        {
            _namespace.text = "Namespace: " + (type.Namespace ?? "Global");
            _depsContainer.Clear();

            foreach (var d in deps)
            {
                var label = new Label("• " + d.Name);
                label.style.color = d.IsInterface ? ArchitectureGraphSettings.InterfaceColor : ArchitectureGraphSettings.ClassColor;
                _depsContainer.Add(label);
            }
        }

        public void ClearInspector()
        {
            _namespace.text = "";
            _depsContainer.Clear();
        }
    }
}
