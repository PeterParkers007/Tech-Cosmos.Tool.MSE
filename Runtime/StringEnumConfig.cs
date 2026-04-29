using System;
using System.Collections.Generic;
using UnityEngine;
namespace TechCosmos.MSE.Runtime
{
    [CreateAssetMenu(fileName = "StringEnumConfig", menuName = "StringEnum/Config")]
    public class StringEnumConfig : ScriptableObject
    {
        public string enumName = "NewStringEnum";
        public string namespaceName = "GeneratedEnums";
        public List<StringEntry> entries = new List<StringEntry>();

        [System.Serializable]
        public class StringEntry
        {
            public string key;      // 枚举成员的名称
            public string value;    // 对应的字符串值
        }
    }
}
