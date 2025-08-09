/*
work by adamman
*/
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AdammanWorkSpace
{
    //BoxInfo Outline Tools
    [AddComponentMenu("UI/Adamman/BoxInfo Outline"), RequireComponent(typeof(Graphic))]
    public class BoxInfoOutline : ModifiedShadow
    {
        const int maxHalfSampleCount = 30; // This is the maximum number of samples we can use for a half box outline.

        [SerializeField]
        [Range(1, maxHalfSampleCount)]
        int m_halfSampleCountX = 1; // The number of samples for the half box outline on the x axis.
        [SerializeField]
        [Range(1, maxHalfSampleCount)]
        int m_halfSampleCountY = 1; // The number of samples for the half box outline on the y axis.

        // The number of samples for the half box outline on the x axis.
        public int halfSampleCountX
        {
            get
            {
                return m_halfSampleCountX;
            }

            set
            {
                m_halfSampleCountX = Mathf.Clamp(value, 1, maxHalfSampleCount);
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        // The number of samples for the half box outline on the y axis.
        public int halfSampleCountY
        {
            get
            {
                return m_halfSampleCountY;
            }

            set
            {
                m_halfSampleCountY = Mathf.Clamp(value, 1, maxHalfSampleCount);
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        // Modify the vertices.
        public override void ModifyVertices(List<UIVertex> verts)
        {
            if (!IsActive())
                return;
            if (m_halfSampleCountY > maxHalfSampleCount)
            {
                m_halfSampleCountY = maxHalfSampleCount;
            }
            if (m_halfSampleCountX > maxHalfSampleCount)
            {
                m_halfSampleCountX = maxHalfSampleCount;
            }

            var neededCapacity = verts.Count * (m_halfSampleCountX * 2 + 1) * (m_halfSampleCountY * 2 + 1);
            if (verts.Capacity < neededCapacity)
                verts.Capacity = neededCapacity;

            var original = verts.Count;
            var count = 0;
            var dx = effectDistance.x / m_halfSampleCountX;
            var dy = effectDistance.y / m_halfSampleCountY;
            for (int x = -m_halfSampleCountX; x <= m_halfSampleCountX; x++)
            {
                for (int y = -m_halfSampleCountY; y <= m_halfSampleCountY; y++)
                {
                    if (!(x == 0 && y == 0))
                    {
                        var next = count + original;
                        ApplyShadow(verts, effectColor, count, next, dx * x, dy * y);
                        count = next;
                    }
                }
            }
        }
    }
}