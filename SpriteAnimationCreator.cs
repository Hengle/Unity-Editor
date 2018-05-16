using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class SpriteAnimationCreator : MonoBehaviour
{
    
    //帧间隔
    private static float defaultInterval = 0.1f;

    //添加菜单
    [MenuItem("Assets/Create/Sprite Animation")]
    public static void Create()
    {
         //获取Project中的Sprite
        List<Sprite> selectedSprites = new List<Sprite>(
            Selection.GetFiltered(typeof(Sprite), SelectionMode.DeepAssets).OfType<Sprite>());

         
        Object[] selectedTextures =
            Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);

        foreach (Object texture in selectedTextures)
        {
            //返回指定路径assetPath下的所有资源对象数组。
            selectedSprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(
                //返回相对于工程资源位置的路径名
                AssetDatabase.GetAssetPath(texture)).OfType<Sprite>());
        }

        // 没有选择精灵的情况
        if (selectedSprites.Count < 1)
        {
            Debug.LogWarning("No sprite selected.");
            return;
        }

        // 排序
        string suffixPattern = "_?([0-9]+)$";
        selectedSprites.Sort((Sprite _1, Sprite _2) => {
            Match match1 = Regex.Match(_1.name, suffixPattern);
            Match match2 = Regex.Match(_2.name, suffixPattern);
           
            if (match1.Success && match2.Success)
            {
                return (int.Parse(match1.Groups[1].Value) -
                    int.Parse(match2.Groups[1].Value));
            }
            //格式不匹配则按照文件名排序
            else
            {
                return _1.name.CompareTo(_2.name);
            }
        });

         
        string baseDir =
            Path.GetDirectoryName(AssetDatabase.GetAssetPath(selectedSprites[0]));
         
        string baseName = Regex.Replace(selectedSprites[0].name, suffixPattern, "");

        if (string.IsNullOrEmpty(baseName))
        {
            baseName = selectedSprites[0].name;
        }

        // 没有画布则创建画布
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.layer = LayerMask.NameToLayer("UI");
        }

        //创建图像
        GameObject obj = new GameObject(baseName);
        obj.transform.parent = canvas.transform;
        obj.transform.localPosition = Vector3.zero;

        Image image = obj.AddComponent<Image>();
        image.sprite = (Sprite)selectedSprites[0];
        image.SetNativeSize();

         
        Animator animator = obj.AddComponent<Animator>();

         
        AnimationClip animationClip =
            AnimatorController.AllocateAnimatorClip(baseName);

 
        EditorCurveBinding editorCurveBinding = new EditorCurveBinding();
        editorCurveBinding.type = typeof(Image);
        editorCurveBinding.path = "";
        editorCurveBinding.propertyName = "m_Sprite";

        
        ObjectReferenceKeyframe[] keyFrames =
            new ObjectReferenceKeyframe[selectedSprites.Count];

        for (int i = 0; i < selectedSprites.Count; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe();
            keyFrames[i].time = i * defaultInterval;
            keyFrames[i].value = selectedSprites[i];
        }

        AnimationUtility.SetObjectReferenceCurve(
            animationClip, editorCurveBinding, keyFrames);

        
        SerializedObject serializedAnimationClip =
            new SerializedObject(animationClip);
        SerializedProperty serializedAnimationClipSettings =
            serializedAnimationClip.FindProperty("m_AnimationClipSettings");
        serializedAnimationClipSettings
            .FindPropertyRelative("m_LoopTime").boolValue = true;
        serializedAnimationClip.ApplyModifiedProperties();

       
        SaveAsset(animationClip, baseDir + "/" + baseName + ".anim");

         
        AnimatorController animatorController =
            AnimatorController.CreateAnimatorControllerAtPathWithClip(
            baseDir + "/" + baseName + ".controller", animationClip);
        animator.runtimeAnimatorController =
            (RuntimeAnimatorController)animatorController;
    }

     
    private static void SaveAsset(Object obj, string path)
    {
        Object existingAsset = AssetDatabase.LoadMainAssetAtPath(path);
        if (existingAsset != null)
        {
            EditorUtility.CopySerialized(obj, existingAsset);
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(obj, path);
        }
    }
}
