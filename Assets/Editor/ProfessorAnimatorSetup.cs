#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ProfessorAnimatorSetup
{
    const string DoneKey = "Fiiverse_ProfessorAnimators_V2";
    const string ModelPath = "Assets/Models/man/rp_eric_rigged_001_yup_a.fbx";
    const string IdleControllerPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/AnimatorControllers/HumanM@Idles.controller";
    const string TalkControllerPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/AnimatorControllers/HumanM@Talking.controller";

    static ProfessorAnimatorSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying)
                return;
            var needsSetup = NeedsSetup();
            if (EditorPrefs.GetBool(DoneKey, false) && !needsSetup)
                return;

            SetupAnimators();
            EditorPrefs.SetBool(DoneKey, true);
        };
    }

    [MenuItem("Tools/Fiiverse/Setup Professor Animators")]
    public static void SetupAnimators()
    {
        EnsureModelImporter();
        var avatar = LoadAvatar(ModelPath);
        var idleController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(IdleControllerPath);
        var talkController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(TalkControllerPath);

        if (avatar == null)
            Debug.LogWarning($"ProfessorAnimatorSetup: Avatar not found at {ModelPath}. Check the model import rig settings.");

        var roots = CollectProfessorRoots();
        if (roots.Count == 0)
            return;

        foreach (var root in roots)
        {
            if (root == null)
                continue;

            CleanupUiControllers(root);
            var animator = FindBestAnimator(root);
            if (animator == null)
            {
                var target = FindAnimatorTarget(root);
                if (target == null)
                    continue;
                animator = target.GetComponent<Animator>();
                if (animator == null)
                    animator = target.AddComponent<Animator>();
            }

            if (avatar != null)
                animator.avatar = avatar;

            if (idleController != null)
                animator.runtimeAnimatorController = idleController;

            var animController = root.GetComponentInChildren<ProfessorAnimationController>(true);
            if (animController == null)
                animController = root.AddComponent<ProfessorAnimationController>();

            animController.idleController = idleController;
            animController.talkController = talkController;
            animController.avatarOverride = avatar;
            animController.SetAnimator(animator);

            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(animController);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    static bool NeedsSetup()
    {
        var roots = CollectProfessorRoots();
        if (roots.Count == 0)
            return false;

        foreach (var root in roots)
        {
            if (root == null)
                continue;
            var target = FindAnimatorTarget(root);
            if (target == null)
                continue;

            var animator = FindBestAnimator(root);
            var animController = root.GetComponentInChildren<ProfessorAnimationController>(true);

            if (animator == null)
                return true;
            if (animator.avatar == null)
                return true;
            if (animator.runtimeAnimatorController == null)
                return true;
            if (animController == null)
                return true;
            if (animController.gameObject.GetComponent<Canvas>() != null || animController.gameObject.GetComponent<RectTransform>() != null)
                return true;
            if (animController.avatarOverride == null)
                return true;
        }

        return false;
    }

    static Avatar LoadAvatar(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var direct = AssetDatabase.LoadAssetAtPath<Avatar>(path);
        if (direct != null)
            return direct;

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model != null)
        {
            var modelAnimator = model.GetComponent<Animator>();
            if (modelAnimator != null && modelAnimator.avatar != null)
                return modelAnimator.avatar;
        }

        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets == null || assets.Length == 0)
            return null;

        return assets.OfType<Avatar>().FirstOrDefault();
    }

    static Animator FindBestAnimator(GameObject root)
    {
        var animators = root.GetComponentsInChildren<Animator>(true);
        foreach (var animator in animators)
        {
            if (animator == null)
                continue;
            if (IsUiAnimator(animator))
                continue;
            return animator;
        }

        return null;
    }

    static bool IsUiAnimator(Animator animator)
    {
        if (animator == null)
            return true;
        if (animator.GetComponent<Canvas>() != null)
            return true;
        if (animator.GetComponent<RectTransform>() != null)
            return true;
        return false;
    }

    static void CleanupUiControllers(GameObject root)
    {
        var controllers = root.GetComponentsInChildren<ProfessorAnimationController>(true);
        foreach (var controller in controllers)
        {
            if (controller == null)
                continue;

            var go = controller.gameObject;
            if (go.GetComponent<Canvas>() == null && go.GetComponent<RectTransform>() == null)
                continue;

            Object.DestroyImmediate(controller);
        }
    }

    static void EnsureModelImporter()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            return;

        var changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    static GameObject FindAnimatorTarget(GameObject root)
    {
        var skinned = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skinned == null)
            return null;

        var rig = skinned.rootBone != null ? skinned.rootBone : skinned.transform;
        var top = rig;
        while (top.parent != null && top.parent != root.transform)
            top = top.parent;

        return top != null ? top.gameObject : skinned.gameObject;
    }

    static HashSet<GameObject> CollectProfessorRoots()
    {
        var roots = new HashSet<GameObject>();

        var chats = Object.FindObjectsOfType<ProfessorChat>(true);
        if (chats != null)
        {
            foreach (var chat in chats)
            {
                if (chat == null)
                    continue;
                var root = ResolveProfessorRoot(chat);
                if (root != null)
                    roots.Add(root);
            }
        }

        var profiles = Object.FindObjectsOfType<ProfessorProfile>(true);
        if (profiles != null)
        {
            foreach (var profile in profiles)
            {
                if (profile == null)
                    continue;
                roots.Add(profile.gameObject);
            }
        }

        return roots;
    }

    static GameObject ResolveProfessorRoot(ProfessorChat chat)
    {
        if (chat == null)
            return null;

        var profile = chat.GetComponentInParent<ProfessorProfile>();
        if (profile != null)
            return profile.gameObject;

        profile = chat.GetComponent<ProfessorProfile>();
        if (profile != null)
            return profile.gameObject;

        return chat.gameObject;
    }
}
#endif
