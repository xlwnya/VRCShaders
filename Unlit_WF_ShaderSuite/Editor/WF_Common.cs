﻿/*
 *  The MIT License
 *
 *  Copyright 2018-2022 whiteflare.
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 *  to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 *  IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 *  TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if UNITY_EDITOR

// #define _WF_LEGACY_FEATURE_SWITCH
// #define WF_COMMON_LOG_KEYWORD // キーワード変更時のログを出力する

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnlitWF
{
    internal static class WFCommonUtility
    {
        /// <summary>
        /// GUIに表示されるDisplayNameのパターン
        /// </summary>
        private static readonly Regex PAT_DISP_NAME = new Regex(@"^\[(?<label>[A-Z][A-Z0-9]*)\]\s+(?<name>.+)$", RegexOptions.Compiled);
        /// <summary>
        /// マテリアルに保存されるプロパティ名のパターン
        /// </summary>
        private static readonly Regex PAT_PROP_NAME = new Regex(@"^_(?<prefix>[A-Z][A-Z0-9]*)_(?<name>.+?)(?<suffix>(?:_\d+)?)$", RegexOptions.Compiled);
        /// <summary>
        /// ENABLEキーワードのパターン
        /// </summary>
        private static readonly Regex PAT_ENABLE_KEYWORD = new Regex(@"^_(?<prefix>[A-Z][A-Z0-9]*)_((?<func>[A-Z0-9]+)_)?ENABLE(?<suffix>(?:_\d+)?)$", RegexOptions.Compiled);

        /// <summary>
        /// プロパティのディスプレイ名から、Prefixと名前を分割する。
        /// </summary>
        /// <param name="text">ディスプレイ名</param>
        /// <param name="label">Prefix</param>
        /// <param name="name">名前</param>
        /// <param name="dispName">ディスプレイ文字列</param>
        /// <returns></returns>
        public static bool FormatDispName(string text, out string label, out string name, out string dispName)
        {
            var mm = PAT_DISP_NAME.Match(text ?? "");
            if (mm.Success)
            {
                label = mm.Groups["label"].Value.ToUpper();
                name = mm.Groups["name"].Value;
                dispName = "[" + label + "] " + name;
                return true;
            }
            else
            {
                label = null;
                name = text;
                dispName = text;
                return false;
            }
        }

        /// <summary>
        /// プロパティ名の文字列から、Prefix+Suffixと名前を分割する。
        /// </summary>
        /// <param name="text">プロパティ名</param>
        /// <param name="label">Prefix+Suffix</param>
        /// <param name="name">名前</param>
        /// <returns></returns>
        public static bool FormatPropName(string text, out string label, out string name)
        {
            var mm = PAT_PROP_NAME.Match(text ?? "");
            if (mm.Success)
            {
                label = mm.Groups["prefix"].Value.ToUpper() + mm.Groups["suffix"].Value.ToUpper();
                name = mm.Groups["name"].Value;
                return true;
            }
            else
            {
                label = null;
                name = text;
                return false;
            }
        }

        /// <summary>
        /// プロパティ物理名からラベル文字列を抽出する。特殊な名称は辞書を参照してラベル文字列を返却する。
        /// </summary>
        /// <param name="prop_name"></param>
        /// <returns></returns>
        public static string GetPrefixFromPropName(string prop_name)
        {
            string label = WFShaderDictionary.SpecialPropNameToLabelMap.GetValueOrNull(prop_name);
            if (label != null)
            {
                return label;
            }
            string name;
            FormatPropName(prop_name, out label, out name);
            return label;
        }

        /// <summary>
        /// プロパティ物理名から Enable トグルかどうかを判定する。
        /// </summary>
        /// <param name="prop_name"></param>
        /// <returns></returns>
        public static bool IsEnableToggleFromPropName(string prop_name)
        {
            string label, name;
            FormatPropName(prop_name, out label, out name);
            return IsEnableToggle(label, name);
        }

        /// <summary>
        /// キーワード文字列が Enable キーワードかどうかを判定する。
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public static bool IsEnableKeyword(string keyword)
        {
            return PAT_ENABLE_KEYWORD.IsMatch(keyword);
        }

        /// <summary>
        /// ラベル＋プロパティ名から Enable トグルかどうかを判定する。
        /// </summary>
        /// <param name="label"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsEnableToggle(string label, string name)
        {
            return label != null && name.ToLower() == "enable";
        }

        public static bool IsPropertyTrue(Material mat, string prop_name)
        {
            return IsPropertyTrue(mat.GetFloat(prop_name));
        }

        public static bool IsPropertyTrue(float value)
        {
            return 0.001f < Math.Abs(value);
        }

        /// <summary>
        /// 見つけ次第削除するシェーダキーワード
        /// </summary>
        private static readonly List<string> DELETE_KEYWORD = new List<string>() {
            "_",
            "_ALPHATEST_ON",
            "_ALPHABLEND_ON",
            "_ALPHAPREMULTIPLY_ON",
        };

        /// <summary>
        /// 各マテリアルのEnableキーワードを設定する
        /// </summary>
        /// <param name="mats"></param>
        public static void SetupShaderKeyword(params Material[] mats)
        {
            // 不要なシェーダキーワードは削除
            foreach (var mat in mats)
            {
                if (!IsSupportedShader(mat))
                {
                    continue;
                }
                foreach (var key in DELETE_KEYWORD)
                {
                    if (mat.IsKeywordEnabled(key))
                    {
                        mat.DisableKeyword(key);
                    }
                }
            }
            // Enableキーワードを整理する
            foreach (var mat in mats)
            {
                if (!IsSupportedShader(mat))
                {
                    continue;
                }
                bool changed = false;
                foreach (var prop_name in WFAccessor.GetAllPropertyNames(mat.shader))
                {
                    // 対応するキーワードが指定されているならばそれを設定する
                    var kwd = WFShaderDictionary.SpecialPropNameToKeywordMap.GetValueOrNull(prop_name);
                    if (kwd != null)
                    {
                        changed |= kwd.SetKeywordTo(mat);
                        continue;
                    }

                    // Enableプロパティならば、それに対応するキーワードを設定する
                    if (IsEnableToggleFromPropName(prop_name))
                    {
                        changed |= new WFCustomKeywordSettingBool(prop_name, prop_name.ToUpper()).SetKeywordTo(mat);
                        continue;
                    }
                }
                if (changed)
                {
#if WF_COMMON_LOG_KEYWORD
                    Debug.LogFormat("[WF] {0} has {1} keywords {2}", mat, mat.shaderKeywords.Length, string.Join(" ", mat.shaderKeywords.OrderBy(k => k)));
#endif
                }
                // _ES_ENABLE に連動して MaterialGlobalIlluminationFlags を設定する
                if (mat.HasProperty("_ES_Enable"))
                {
                    var flag = mat.GetInt("_ES_Enable") != 0 ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.None;
                    if (mat.globalIlluminationFlags != flag)
                    {
                        mat.globalIlluminationFlags = flag;
                    }
                }
            }
        }

        /// <summary>
        /// マテリアルの shader を指定の名前のものに変更する。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mats"></param>
        public static void ChangeShader(string name, params Material[] mats)
        {
            if (string.IsNullOrWhiteSpace(name) || mats.Length == 0)
            {
                return; // なにもしない
            }
            var newShader = FindShader(name);
            if (newShader != null)
            {
                Undo.RecordObjects(mats, "change shader");
                foreach (var m in mats)
                {
                    if (m == null)
                    {
                        continue;
                    }
                    var oldM = new Material(m);

                    // 初期化処理の呼び出し (カスタムエディタを取得してAssignNewShaderToMaterialしたかったけど手が届かなかったので静的アクセス)
                    if (WF_DebugViewEditor.IsSupportedShader(newShader))
                    {
                        WF_DebugViewEditor.PreChangeShader(m, oldM.shader, newShader);
                    }
                    else if (ShaderCustomEditor.IsSupportedShader(newShader))
                    {
                        ShaderCustomEditor.PreChangeShader(m, oldM.shader, newShader);
                    }
                    // マテリアルにシェーダ割り当て
                    m.shader = newShader;
                    // 初期化処理の呼び出し (カスタムエディタを取得してAssignNewShaderToMaterialしたかったけど手が届かなかったので静的アクセス)
                    if (WF_DebugViewEditor.IsSupportedShader(newShader))
                    {
                        WF_DebugViewEditor.PostChangeShader(oldM, m, oldM.shader, newShader);
                    }
                    else if (ShaderCustomEditor.IsSupportedShader(newShader))
                    {
                        ShaderCustomEditor.PostChangeShader(oldM, m, oldM.shader, newShader);
                    }
                }
            }
            else
            {
                Debug.LogErrorFormat("[WF][Common] Shader Not Found in this projects: {0}", name);
            }
        }

        /// <summary>
        /// 指定名称の Shader を検索して返す。
        /// もし名称が UnlitWF/ で始まる場合、アセットパスを元に並び替えて先頭の Shader を返却する。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Shader FindShader(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            if (!name.StartsWith("UnlitWF/"))
            {
                // もしシェーダ名が UnlitWF/ で始まらないならばサポート外シェーダへの切り替えなので通常の Shader.Find を使う
                // IsSupportedShader はここでは使わない (Custom/UnlitWF/ のように ShaderCustomEditor を使う独自シェーダはこちらに倒す)
                return Shader.Find(name);
            }

            // 全シェーダをロードしてしまうとAssetDatabase.FindAssetsでも高速に切り替えられるのでGetAllShaderInfoは使用しない
            //var cnt = ShaderUtil.GetAllShaderInfo().Count(si => si.name == name);
            //if (cnt <= 1)
            //{
            //    // もし同一名称の Shader が複数存在しないのであれば、通常の Shader.Find を使う
            //    return Shader.Find(name);
            //}

            // 同一名称の Shader からひとつを選択する
            var shaders = AssetDatabase.FindAssets("t:Shader")
                // パスを取得
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Where(path => !string.IsNullOrWhiteSpace(path))
                // シェーダをロード
                .Select(path => new ShaderAndAssetPath(path, AssetDatabase.LoadAssetAtPath<Shader>(path))).Where(sp => sp.shader != null)
                // 名称の一致するシェーダのみリストアップする
                .Where(sp => sp.shader.name == name)
                // 優先度順に並び替える
                .OrderBy(sp => sp).ToArray();
            if (shaders.Length == 0)
            {
                return null;
            }
            if (2 <= shaders.Length)
            {
                string msg = "[WF][Common] Multiple Shaders hit: name = " + name;
                foreach(var sh in shaders)
                {
                    msg += "\n  " + sh.path;
                }
                Debug.LogWarning(msg);
            }

            return shaders[0].shader;
        }

        private class ShaderAndAssetPath : IComparable<ShaderAndAssetPath>
        {
            public readonly string path;
            public readonly Shader shader;

            private readonly bool isMatch;
            private readonly int root; // 0:Packages, 1:Assets
            private readonly string parent;
            private readonly string folder;
            private readonly string tail;

            public ShaderAndAssetPath(string path, Shader shader)
            {
                this.path = path;
                this.shader = shader;

                this.root = path.StartsWith("Packages/") ? 0 : 1;

                var mm = pattern.Match(path);
                this.isMatch = mm.Success;
                if (isMatch)
                {
                    this.parent = mm.Groups["parent"].Value;
                    this.folder = mm.Groups["folder"].Value;
                    this.tail = mm.Groups["tail"].Value;
                }
                else
                {
                    this.parent = "";
                    this.folder = "";
                    this.tail = path;
                }
            }

            private static readonly Regex pattern = new Regex(@"^(?<root>(?:Packages|Assets)(?<parent>/[^/]+)*)/(?<folder>Unlit_?WF_?Shader[A-Za-z]*)/(?<tail>.*)$", RegexOptions.Compiled);

            public int CompareTo(ShaderAndAssetPath other)
            {
                int ret;

                // マッチするものはマッチしないものよりも優先
                ret = -this.isMatch.CompareTo(other.isMatch);
                if (ret != 0)
                {
                    return ret;
                }

                // Packages は Assets よりも優先
                ret = -this.root.CompareTo(other.root);
                if (ret != 0)
                {
                    return ret;
                }

                // parent が浅いものほど優先
                ret = this.parent.Count(c => c == '/').CompareTo(other.parent.Count(c => c == '/'));
                if (ret != 0)
                {
                    return ret;
                }

                // parent + folder の辞書順で比較
                ret = (this.parent + "/" + this.folder).CompareTo(other.parent + "/" + other.folder);
                if (ret != 0)
                {
                    return ret;
                }

                // tail の長さ順で比較
                ret = this.tail.Length.CompareTo(other.tail.Length);
                if (ret != 0)
                {
                    return ret;
                }

                // tail の辞書順で比較
                return this.tail.CompareTo(other.tail);
            }
        }

        /// <summary>
        /// Object[] -> Material[] のユーティリティ関数。
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static Material[] AsMaterials(params UnityEngine.Object[] array)
        {
            return array == null ? new Material[0] : array.Select(obj => obj as Material).Where(m => m != null).ToArray();
        }

        /// <summary>
        /// ShaderがUnlitWFでサポートされるものかどうか判定する。
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public static bool IsSupportedShader(Shader shader)
        {
            return shader != null && shader.name.Contains("UnlitWF");
        }

        /// <summary>
        /// ShaderがUnlitWFでサポートされるものかどうか判定する。
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static bool IsSupportedShader(Material mat)
        {
            return mat != null && IsSupportedShader(mat.shader);
        }

        /// <summary>
        /// ShaderがVRC QuestでサポートされるUnlitWFかどうか判定する。
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public static bool IsMobileSupportedShader(Shader shader)
        {
            if (!IsSupportedShader(shader))
            {
                return false;
            }
            var name = shader.name;
            if (name.Contains("_Mobile_") || name.Contains("WF_UnToon_Hidden") || name.Contains("WF_DebugView"))
            {
                return true;
            }
            if (WFAccessor.GetShaderQuestSupported(shader))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// ShaderがVRC QuestでサポートされるUnlitWFかどうか判定する。
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static bool IsMobileSupportedShader(Material mat)
        {
            return mat != null && IsMobileSupportedShader(mat.shader);
        }

        public static bool IsMigrationRequiredMaterial(Material mat)
        {
            return mat != null && Converter.WFMaterialMigrationConverter.ExistsNeedsMigration(mat);
        }

        /// <summary>
        /// 最新リリースのVersionInfo
        /// </summary>
        private static WFVersionInfo LatestVersion = null;

        /// <summary>
        /// 最新リリースのVersionInfoを返却する。不明のときはnullを返却する。
        /// </summary>
        /// <returns></returns>
        public static WFVersionInfo GetLatestVersion()
        {
            return LatestVersion;
        }

        /// <summary>
        /// 最新リリースのVersionInfoを設定する。
        /// </summary>
        /// <param name="ver"></param>
        public static void SetLatestVersion(WFVersionInfo ver)
        {
            LatestVersion = ver != null && ver.HasValue() ? ver : null;
        }

        /// <summary>
        /// 指定のバージョン文字列が最新リリースよりも古いかどうか判定する。不明のときはfalseを返す。
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static bool IsOlderShaderVersion(string version)
        {
            if (LatestVersion == null || version == null)
            {
                return false;
            }
            return version.CompareTo(LatestVersion.latestVersion) < 0;
        }

        /// <summary>
        /// 最新リリースのダウンロードページを開く。
        /// </summary>
        public static void OpenDownloadPage()
        {
            if (LatestVersion == null)
            {
                return;
            }
            Application.OpenURL(LatestVersion.downloadPage);
        }

        [Obsolete("use WFAccessor")]
        public static IEnumerable<string> GetAllPropertyNames(Shader shader)
        {
            return WFAccessor.GetAllPropertyNames(shader);
        }

        [Obsolete("use WFAccessor")]
        public static string GetShaderCurrentVersion(Shader shader)
        {
            return WFAccessor.GetShaderCurrentVersion(shader);
        }

        [Obsolete("use WFAccessor")]
        public static string GetShaderCurrentVersion(Material mat)
        {
            return WFAccessor.GetShaderCurrentVersion(mat);
        }

        [Obsolete("use WFAccessor")]
        public static string GetShaderFallBackTarget(Shader shader)
        {
            return WFAccessor.GetShaderFallBackTarget(shader);
        }

        [Obsolete("use WFAccessor")]
        public static string GetShaderFallBackTarget(Material mat)
        {
            return WFAccessor.GetShaderFallBackTarget(mat);
        }

        [Obsolete("use WFAccessor")]
        public static bool GetShaderQuestSupported(Shader shader)
        {
            return WFAccessor.GetShaderQuestSupported(shader);
        }

        [Obsolete("use WFAccessor")]
        public static int GetMaterialRenderQueueValue(Material mat)
        {
            return WFAccessor.GetMaterialRenderQueueValue(mat);
        }

        public static string GetCurrentRenderPipeline()
        {
            return WFCommonUtility.IsURP() ? "URP" : "BRP";
        }

        public static bool IsURP()
        {
#if UNITY_2019_1_OR_NEWER
            return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
#else
            return false;
#endif
        }

        public const string KWD_EDITOR_HIDE_LMAP = "_WF_EDITOR_HIDE_LMAP";

        public static bool IsKwdEnableHideLmap()
        {
            return Shader.IsKeywordEnabled(KWD_EDITOR_HIDE_LMAP);
        }

        public static void SetKwdEnableHideLmap(bool value)
        {
            if (value)
            {
                Shader.EnableKeyword(KWD_EDITOR_HIDE_LMAP);
            }
            else
            {
                Shader.DisableKeyword(KWD_EDITOR_HIDE_LMAP);
            }
        }
    }

    public static class WFAccessor
    {
        /// <summary>
        /// Shader に指定のプロパティが存在するかどうか返す。
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool HasShaderProperty(Shader shader, string name)
        {
            return 0 <= findPropertyIndex(shader, name);
        }

        /// <summary>
        /// Shader に指定のプロパティが Texture タイプで存在するかどうか返す。
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool HasShaderPropertyTexture(Shader shader, string name)
        {
            var idx = findPropertyIndex(shader, name);
            if (idx < 0)
            {
                return false;
            }
#if UNITY_2019_1_OR_NEWER
            return shader.GetPropertyType(idx) == UnityEngine.Rendering.ShaderPropertyType.Texture;
#else
            return ShaderUtil.GetPropertyType(shader, idx) == ShaderUtil.ShaderPropertyType.TexEnv;
#endif
        }

        /// <summary>
        /// Shader の全てのプロパティ名を返す。
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllPropertyNames(Shader shader)
        {
#if UNITY_2019_1_OR_NEWER
            for (int idx = 0; idx < shader.GetPropertyCount(); idx++)
            {
                yield return shader.GetPropertyName(idx);
            }
#else
            for (int idx = ShaderUtil.GetPropertyCount(shader) - 1; 0 <= idx; idx--)
            {
                yield return ShaderUtil.GetPropertyName(shader, idx);
            }
#endif
        }

        /// <summary>
        /// Shader から 指定の名前のプロパティの PropertyIndex を取得する。
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static int findPropertyIndex(Shader shader, string name)
        {
#if UNITY_2019_1_OR_NEWER
            return shader.FindPropertyIndex(name);
#else
            for (int idx = ShaderUtil.GetPropertyCount(shader) - 1; 0 <= idx; idx--)
            {
                if (name == ShaderUtil.GetPropertyName(shader, idx))
                {
                    return idx;
                }
            }
            return -1;
#endif
        }

        /// <summary>
        /// Shader から 指定の名前のプロパティの description を取得する。
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string getPropertyDescription(Shader shader, string name, string _default = null)
        {
            var idx = findPropertyIndex(shader, name);
            if (0 <= idx)
            {
#if UNITY_2019_1_OR_NEWER
                return shader.GetPropertyDescription(idx);
#else
                return ShaderUtil.GetPropertyDescription(shader, idx);
#endif
            }
            return _default;
        }

        /// <summary>
        /// Shader から _CurrentVersion の値を取得する。
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public static string GetShaderCurrentVersion(Shader shader)
        {
            return getPropertyDescription(shader, "_CurrentVersion");
        }

        /// <summary>
        /// Shader から _FallBack の値を取得する。
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public static string GetShaderFallBackTarget(Shader shader)
        {
            return getPropertyDescription(shader, "_FallBack");
        }

        /// <summary>
        /// Shader から QuestSupported の値を取得する。
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public static bool GetShaderQuestSupported(Shader shader)
        {
            return getPropertyDescription(shader, "_QuestSupported", "false").ToLower() == "true";
        }


        /// <summary>
        /// Material から _CurrentVersion の値を取得する。
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static string GetShaderCurrentVersion(Material mat)
        {
            return mat == null ? null : GetShaderCurrentVersion(mat.shader);
        }

        /// <summary>
        /// Material から _FallBack の値を取得する。
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static string GetShaderFallBackTarget(Material mat)
        {
            return mat == null ? null : GetShaderFallBackTarget(mat.shader);
        }

        public static int GetMaterialRenderQueueValue(Material mat)
        {
            // Material.renderQueue の値を単に参照すると -1 (FromShader) が取れないので SerializedObject から取得する
            var so = new SerializedObject(mat);
            so.Update();
            var prop = so.FindProperty("m_CustomRenderQueue");
            if (prop != null)
            {
                return prop.intValue;
            }
            return mat.renderQueue;
        }

        public static string GetMaterialRenderType(Material mat)
        {
            return mat.GetTag("RenderType", true, "Opaque");
        }

        public static bool IsMaterialRenderType(Material mat, params string[] tags)
        {
            return tags.Contains(GetMaterialRenderType(mat));
        }

        public static int GetInt(Material mat, string name, int _default)
        {
            if (mat.HasProperty(name))
            {
                return mat.GetInt(name);
            }
            return _default;
        }

        public static float GetFloat(Material mat, string name, float _default)
        {
            if (mat.HasProperty(name))
            {
                return mat.GetFloat(name);
            }
            return _default;
        }
    }

    public abstract class WFCustomKeywordSetting
    {
        public readonly string propertyName;
        public string enablePropName;

        protected WFCustomKeywordSetting(string propertyName)
        {
            this.propertyName = propertyName;
        }

        public abstract bool SetKeywordTo(Material mat);

        protected bool IsEnable(Material mat)
        {
            if (string.IsNullOrWhiteSpace(enablePropName))
            {
                // 未指定のときはOK
                return true;
            }
            return WFCommonUtility.IsPropertyTrue(mat, enablePropName);
        }

        protected bool ApplyKeyword(Material mat, string[] kwds, int value)
        {
            bool enable = IsEnable(mat);
            bool changed = false;
            for (int i = 0; i < kwds.Length; i++)
            {
                changed |= SetKeyword(mat, kwds[i], enable && i == value);
            }
            return changed;
        }

        public bool ApplyKeywordByBool(Material mat, string kwd, bool value)
        {
            bool enable = IsEnable(mat);
            return SetKeyword(mat, kwd, enable && value);
        }

        public static bool SetKeyword(Material mat, string kwd, bool value)
        {
#if !UNITY_2019_1_OR_NEWER || _WF_LEGACY_FEATURE_SWITCH
            // 旧版では常に false として扱う。これにより既にマテリアルに設定されていたキーワードは2018で消去される。
            value = false;
#endif
            if (string.IsNullOrEmpty(kwd) || kwd == "_" || mat.IsKeywordEnabled(kwd) == value)
            {
                return false;
            }
            if (value)
            {
                mat.EnableKeyword(kwd);
            }
            else
            {
                mat.DisableKeyword(kwd);
            }
            return true;
        }
    }

    public class WFCustomKeywordSettingBool : WFCustomKeywordSetting
    {
        public readonly string keyword;

        public WFCustomKeywordSettingBool(string propertyName, string keyword) : base(propertyName)
        {
            this.keyword = keyword;
        }

        public override bool SetKeywordTo(Material mat)
        {
            return ApplyKeywordByBool(mat, keyword, WFCommonUtility.IsPropertyTrue(mat, propertyName));
        }
    }

    public class WFCustomKeywordSettingEnum : WFCustomKeywordSetting
    {
        public readonly string[] keywords;
        public readonly int[] index;

        public WFCustomKeywordSettingEnum(string propertyName, params string[] keywords) : base(propertyName)
        {
            // キーワードの配列を重複なしに変換し、インデックスの変換表を作成する
            var newKwdArray = new List<string>();
            var newIdxArray = new List<int>();
            for (int i = 0; i < keywords.Length; i++)
            {
                string kwd = keywords[i];
                int idx = newKwdArray.IndexOf(kwd);
                if (idx < 0)
                {
                    idx = newKwdArray.Count;
                    newKwdArray.Add(kwd);
                }
                newIdxArray.Add(idx);
            }

            this.keywords = newKwdArray.ToArray();
            this.index = newIdxArray.ToArray();
        }

        public override bool SetKeywordTo(Material mat)
        {
            int value = mat.GetInt(propertyName);
            value = 0 <= value && value < index.Length ? index[value] : -1;
            return ApplyKeyword(mat, keywords, value);
        }
    }

    [Serializable]
    public class WFVersionInfo
    {
        public string latestVersion;
        public string downloadPage;

        public bool HasValue()
        {
            return latestVersion != null && downloadPage != null;
        }
    }

    public class WFShaderFunction
    {
        private static List<string> uniqueLabel = new List<string>();

        public readonly string Label;
        public readonly string Prefix;
        public readonly string Name;
        private readonly Func<WFShaderFunction, Material, bool> _contains;

        internal WFShaderFunction(string label, string prefix, string name) : this(label, prefix, name, IsEnable)
        {
        }

        public static bool IsEnable(WFShaderFunction func, Material mat)
        {
            return IsEnable("_" + func.Prefix + "_Enable", mat);
        }

        public static bool IsEnable(string name, Material mat)
        {
            if (mat.HasProperty(name))
            {
                return mat.GetInt(name) != 0;
            }
            return false;
        }

        internal WFShaderFunction(string label, string prefix, string name, Func<WFShaderFunction, Material, bool> contains)
        {
            Label = label;
            Prefix = prefix;
            Name = name;
            _contains = contains;

            if (uniqueLabel.Contains(Label))
            {
                Debug.LogWarningFormat("[WF][Common] WFShaderFunction duplicate Label: " + Label);
            }
            else
            {
                uniqueLabel.Add(Label);
            }
        }

        public bool IsEnable(Material mat)
        {
            if (!WFCommonUtility.IsSupportedShader(mat))
            {
                return false;
            }
            return _contains(this, mat);
        }

        public static List<string> LabelToPrefix(List<string> labelList)
        {
            return labelList.Select(LabelToPrefix).Where(prefix => prefix != null).Distinct().ToList();
        }

        public static string LabelToPrefix(string label)
        {
            return WFShaderDictionary.ShaderFuncList.Where(func => func.Label == label).Select(func => func.Prefix).FirstOrDefault();
        }

        public static WFShaderFunction[] GetEnableFunctionList(Material mat)
        {
            var result = new List<WFShaderFunction>();
            foreach (var func in WFShaderDictionary.ShaderFuncList)
            {
                if (func.IsEnable(mat))
                {
                    result.Add(func);
                }
            }
            return result.ToArray();
        }
    }

    internal enum EditorLanguage
    {
        English, 日本語, 한국어
    }

    internal class WFI18NTranslation
    {
        public readonly string Before;
        public readonly string After;
        private readonly HashSet<string> Tags = new HashSet<string>();

        public WFI18NTranslation(string before, string after) : this(null, before, after)
        {
        }

        public WFI18NTranslation(string tag, string before, string after)
        {
            Before = before;
            After = after;
            AddTag(tag);
        }

        public WFI18NTranslation AddTag(params string[] tags)
        {
            foreach (var tag in tags)
            {
                if (tag != null)
                {
                    Tags.Add(tag);
                }
            }
            return this;
        }

        public bool ContainsTag(string tag)
        {
            return tag != null && (Tags.Count == 0 || Tags.Contains(tag));
        }

        public bool HasNoTag()
        {
            return Tags.Count == 0;
        }
    }

    internal static class WFEditorPrefs
    {
        private static readonly string KEY_EDITOR_LANG = "UnlitWF.ShaderEditor/Lang";
        private static readonly string KEY_MENU_TO_BOTTOM = "UnlitWF.ShaderEditor/MenuToBottom";

        private static bool? menuToBottom = null;
        private static EditorLanguage? langMode = null;

        public static bool MenuToBottom
        {
            get
            {
                if (menuToBottom == null)
                {
                    menuToBottom = EditorPrefs.GetBool(KEY_MENU_TO_BOTTOM, false);
                }
                return menuToBottom.Value;
            }
            set
            {
                if (menuToBottom != value)
                {
                    menuToBottom = value;
                    if (value)
                    {
                        EditorPrefs.SetBool(KEY_MENU_TO_BOTTOM, true);
                    }
                    else
                    {
                        EditorPrefs.DeleteKey(KEY_MENU_TO_BOTTOM);
                    }
                }
            }
        }

        public static EditorLanguage LangMode
        {
            get
            {
                if (langMode == null)
                {
                    string lang = EditorPrefs.GetString(KEY_EDITOR_LANG) ?? "";
                    if (!string.IsNullOrWhiteSpace(lang))
                    {
                        return ToEditorLanguage(lang);
                    }
                    langMode = ToEditorLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName);
                }
                return langMode.Value;
            }
            set
            {
                langMode = value;
                SetLangModeInternal(langMode);
            }
        }

        private static EditorLanguage ToEditorLanguage(string lang)
        {
            switch (lang)
            {
                case "ja":
                    return EditorLanguage.日本語;
                case "ko":
                    return EditorLanguage.한국어;
                default:
                    return EditorLanguage.English;
            }
        }

        private static void SetLangModeInternal(EditorLanguage? value)
        {
            if (value == null)
            {
                EditorPrefs.DeleteKey(KEY_EDITOR_LANG);
            }
            else
            {
                switch (value)
                {
                    case EditorLanguage.日本語:
                        EditorPrefs.SetString(KEY_EDITOR_LANG, "ja");
                        break;
                    case EditorLanguage.한국어:
                        EditorPrefs.SetString(KEY_EDITOR_LANG, "ko");
                        break;
                    default:
                        EditorPrefs.SetString(KEY_EDITOR_LANG, "en");
                        break;
                }
            }
        }
    }

    internal static class WFI18N
    {
        private static readonly Dictionary<string, List<WFI18NTranslation>> EN = new Dictionary<string, List<WFI18NTranslation>>();
        private static readonly Dictionary<string, List<WFI18NTranslation>> JA = ToDict(WFShaderDictionary.LangEnToJa);
        private static readonly Dictionary<string, List<WFI18NTranslation>> KO = ToDict(WFShaderDictionary.LangEnToKo);

        static Dictionary<string, List<WFI18NTranslation>> GetDict()
        {
            switch (WFEditorPrefs.LangMode)
            {
                case EditorLanguage.日本語:
                    return JA;
                case EditorLanguage.한국어:
                    return KO;
                default:
                    return EN;
            }
        }

        public static string Translate(string before)
        {
            before = before ?? "";

            var current = GetDict();
            if (current == null || current.Count == 0)
            {
                return before; // 無いなら変換しない
            }

            // ラベルなしでテキストが一致するものを検索する
            if (current.TryGetValue(before, out var list))
            {
                var after = list.Where(t => t.HasNoTag()).Select(t => t.After).FirstOrDefault();
                if (after != null)
                {
                    return after;
                }
            }
            // マッチするものがないなら変換しない
            return before;
        }

        public static string Translate(string label, string before)
        {
            before = before ?? "";

            var current = GetDict();
            if (current == null || current.Count == 0)
            {
                return before; // 無いなら変換しない
            }

            // テキストと一致する変換のなかからラベルも一致するものを翻訳にする
            if (current.TryGetValue(before, out var list))
            {
                var after = list.Where(t => t.ContainsTag(label)).Select(t => t.After).FirstOrDefault();
                if (after != null)
                {
                    return after;
                }
            }
            // マッチするものがないなら変換しない
            return before;
        }

        private static string SplitAndTranslate(string before)
        {
            if (WFCommonUtility.FormatDispName(before, out var label, out var text, out var _))
            {
                // text がラベルとテキストに分割できるならば
                return "[" + label + "] " + Translate(label, text);
            }
            else
            {
                // そうでなければ
                return Translate(before);
            }
        }

        public static GUIContent GetGUIContent(string text)
        {
            var localized = SplitAndTranslate(text);
            return new GUIContent(localized);
        }

        public static GUIContent GetGUIContent(string label, string text, string tooltip = null)
        {
            string localized = Translate(label, text);
            if (string.IsNullOrWhiteSpace(tooltip))
            {
                return new GUIContent("[" + label + "] " + localized);
            }
            return new GUIContent("[" + label + "] " + localized, tooltip);
        }

        private static Dictionary<string, List<WFI18NTranslation>> ToDict(List<WFI18NTranslation> from)
        {
            var result = new Dictionary<string, List<WFI18NTranslation>>();

            foreach (var group in from.GroupBy(t => t.Before))
            {
                result[group.Key] = new List<WFI18NTranslation>(group.OrderBy(t => t.HasNoTag()));
            }

            return result;
        }
    }

    internal class WFShaderName
    {
        public readonly string RenderPipeline;
        public readonly string Familly;
        public readonly string Variant;
        public readonly string RenderType;
        public readonly string Name;
        public readonly bool Represent;

        public WFShaderName(string rp, string familly, string variant, string renderType, string name, bool represent = false)
        {
            RenderPipeline = rp;
            Familly = familly;
            Variant = variant;
            RenderType = renderType;
            Name = name;
            Represent = represent;
        }
    }

    internal class WFShaderNameVariantComparer : IEqualityComparer<WFShaderName>
    {
        public bool Equals(WFShaderName x, WFShaderName y)
        {
            if (System.Object.ReferenceEquals(x, y))
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            return x.RenderPipeline == y.RenderPipeline && x.Familly == y.Familly && x.Variant == y.Variant;
        }

        public int GetHashCode(WFShaderName x)
        {
            if (System.Object.ReferenceEquals(x, null))
            {
                return 0;
            }
            return x.RenderPipeline.GetHashCode() ^ x.Familly.GetHashCode() ^ x.Variant.GetHashCode();
        }
    }

    internal class WFShaderNameRenderTypeComparer : IEqualityComparer<WFShaderName>
    {
        public bool Equals(WFShaderName x, WFShaderName y)
        {
            if (System.Object.ReferenceEquals(x, y))
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            return x.RenderPipeline == y.RenderPipeline && x.Familly == y.Familly && x.RenderType == y.RenderType;
        }

        public int GetHashCode(WFShaderName x)
        {
            if (System.Object.ReferenceEquals(x, null))
            {
                return 0;
            }
            return x.RenderPipeline.GetHashCode() ^ x.Familly.GetHashCode() ^ x.Variant.GetHashCode();
        }
    }

    internal static class WFShaderNameDictionary
    {
        private static IEnumerable<WFShaderName> GetCurrentRpNames()
        {
            var rp = WFCommonUtility.GetCurrentRenderPipeline();
            return WFShaderDictionary.ShaderNameList.Where(nm => nm.RenderPipeline == rp);
        }

        public static WFShaderName TryFindFromName(string name)
        {
            return GetCurrentRpNames().Where(nm => nm.Name == name).FirstOrDefault();
        }

        public static List<WFShaderName> GetFamilyList()
        {
            var result = new List<WFShaderName>();
            foreach(var group in GetCurrentRpNames().GroupBy(p => p.Familly))
            {
                // Family ごとにグループ化して、Represent が true のものがあればそれを取得、そうでなければ最初の1件を取得してリストに詰める
                var represent = group.Where(p => p.Represent).Union(group).FirstOrDefault();
                if (represent != null)
                {
                    result.Add(represent);
                }
            }
            return result;
        }

        public static List<WFShaderName> GetVariantList(WFShaderName name)
        {
            if (name == null)
            {
                return new List<WFShaderName>();
            }
            return GetCurrentRpNames().Where(nm => nm.Familly == name.Familly && nm.RenderType == name.RenderType).ToList();
        }

        public static List<WFShaderName> GetVariantList(WFShaderName name, out List<WFShaderName> other)
        {
            var result = GetVariantList(name);
            var items = result.Select(p => p.Variant).ToList();

            // 異なるVariantのShaderNameをotherに詰める
            other = new List<WFShaderName>();
            other.AddRange(GetCurrentRpNames().Where(nm => nm.Familly == name.Familly));
            other.RemoveAll(a => items.Contains(a.Variant));

            return result;
        }

        public static List<WFShaderName> GetRenderTypeList(WFShaderName name)
        {
            if (name == null)
            {
                return new List<WFShaderName>();
            }
            return GetCurrentRpNames().Where(nm => nm.Familly == name.Familly && nm.Variant == name.Variant).ToList();
        }

        public static List<WFShaderName> GetRenderTypeList(WFShaderName name, out List<WFShaderName> other)
        {
            var result = GetRenderTypeList(name);
            var items = result.Select(p => p.RenderType).ToList();

            // 異なるRenderTypeのShaderNameをotherに詰める
            other = new List<WFShaderName>();
            other.AddRange(GetCurrentRpNames().Where(nm => nm.Familly == name.Familly && nm.Variant != "Custom"));
            other.RemoveAll(a => items.Contains(a.RenderType));

            return result;
        }
    }
}

#endif
