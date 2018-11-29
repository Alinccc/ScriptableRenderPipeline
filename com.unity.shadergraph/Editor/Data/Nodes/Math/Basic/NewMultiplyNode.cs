using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class NewMultiplyNode : IShaderNodeType
    {
        InputPortRef m_APort;
        InputPortRef m_BPort;
        OutputPortRef m_OutPort;

        public void Setup(ref NodeSetupContext context)
        {
            m_APort = context.CreateInputPort(0, "A", PortValue.Vector1(0.5f));
            m_BPort = context.CreateInputPort(1, "B", PortValue.Vector1(0.5f));
            m_OutPort = context.CreateOutputPort(2, "Out", PortValueType.Vector1);

            var type = new NodeTypeDescriptor
            {
                path = "Math/Basic",
                name = "New Multiply",
                inputs = new List<InputPortRef> { m_APort, m_BPort },
                outputs = new List<OutputPortRef> { m_OutPort }
            };

            context.CreateType(type);
        }

        HlslSourceRef m_Source;

        public void OnChange(ref NodeTypeChangeContext context)
        {
            // TODO: Figure out what should cause the user to create the hlsl source
            // TODO: How does sharing files between multiple node types work?
            if (!m_Source.isValid)
            {
                m_Source = context.CreateHlslSource("Packages/com.unity.shadergraph/Editor/Data/Nodes/Math/Basic/Math_Basic.hlsl");
            }

            foreach (var node in context.addedNodes)
            {
                context.SetHlslFunction(node, new HlslFunctionDescriptor
                {
                    source = m_Source,
                    name = "Unity_Multiply",
                    arguments = new HlslArgumentList { m_APort, m_BPort },
                    returnValue = m_OutPort
                });
            }
        }
    }
}
