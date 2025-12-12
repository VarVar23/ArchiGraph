using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArchiGraph
{
    public class ArchitectureNodeView : Node
    {
        public Action OnNodeSelected;
        public Action OnNodeDeselected;
        public Action RefreshColors;

        public Port InputPort;

        private bool _isInterface;
        private Label _titleLabel;

        public ArchitectureNodeView(string titleText, bool isInterface)
        {
            _isInterface = isInterface;

            title = titleText;

            _titleLabel = titleContainer.Q<Label>();
            if (_titleLabel == null)
                _titleLabel = titleContainer.Q<VisualElement>()?.Q<Label>();

            InputPort = InstantiatePort(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi,
                typeof(bool)
            );

            InputPort.portName = "";
            inputContainer.Add(InputPort);

            RefreshColors = ApplyColors;
        }

        public void SetIsInterface(bool isInterface)
        {
            _isInterface = isInterface;
        }

        private void ApplyColors()
        {
            Color interfaceColor = ArchitectureGraphSettings.InterfaceColor;
            Color classColor = ArchitectureGraphSettings.ClassColor;
            Color defaultInputColor = ArchitectureGraphSettings.InputColor;

            Color inputColor = _isInterface ? interfaceColor : defaultInputColor;

            InputPort.portColor = inputColor;

            var inLabel = InputPort.Q<Label>();
            if (inLabel != null)
            {
                inLabel.style.color = inputColor;
                inLabel.EnableInClassList("unity-disabled", false);
            }

            foreach (var child in outputContainer.Children())
            {
                if (child is Port p)
                {
                    bool isInterfaceDep = p.userData is bool b && b;

                    Color c = isInterfaceDep ? interfaceColor : classColor;

                    p.portColor = c;

                    var lab = p.Q<Label>();
                    if (lab != null)
                    {
                        lab.style.color = c;
                        lab.EnableInClassList("unity-disabled", false);
                    }
                }
            }

            if (_titleLabel != null)
            {
                _titleLabel.style.color = _isInterface ? interfaceColor : classColor;
                _titleLabel.EnableInClassList("unity-disabled", false);
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            OnNodeDeselected?.Invoke();
        }
    }
}
