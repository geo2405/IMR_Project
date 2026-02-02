using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ProfessorAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    public bool autoResolveAnimator = true;
    public Avatar avatarOverride;

    [Header("Controller Swap (optional)")]
    public RuntimeAnimatorController idleController;
    public RuntimeAnimatorController talkController;

    [Header("Parameters / States")]
    public string boolParameter = "IsTalking";
    public string idleState = "Idle";
    public string talkState = "Talk";
    public float crossFade = 0.15f;

#if UNITY_EDITOR
    const string DefaultIdleControllerPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/AnimatorControllers/HumanM@Idles.controller";
    const string DefaultTalkControllerPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/AnimatorControllers/HumanM@Talking.controller";
#endif

    private bool hasBoolParameter;

    void Awake()
    {
        if (autoResolveAnimator)
            ResolveAnimator();
        else if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheParameters();
    }

    public void SetTalking(bool talking)
    {
        if (IsAnimatorInvalid(animator))
            ResolveAnimator();
        if (animator == null)
            return;

        if (idleController != null && talkController != null)
        {
            animator.runtimeAnimatorController = talking ? talkController : idleController;
            return;
        }

        if (hasBoolParameter)
        {
            animator.SetBool(boolParameter, talking);
            return;
        }

        var stateName = talking ? talkState : idleState;
        if (!string.IsNullOrEmpty(stateName))
            animator.CrossFade(stateName, crossFade);
    }

    public void SetAnimator(Animator value)
    {
        animator = value;
        if (animator != null)
        {
            if (avatarOverride != null && animator.avatar == null)
                animator.avatar = avatarOverride;
            if (animator.runtimeAnimatorController == null && idleController != null)
                animator.runtimeAnimatorController = idleController;
        }
        CacheParameters();
    }

    private void ResolveAnimator()
    {
        if (IsAnimatorInvalid(animator))
        {
            var found = FindBestAnimator();
            if (found != null)
                animator = found;
        }

        if (animator == null)
        {
            var skinned = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinned != null)
                animator = skinned.gameObject.AddComponent<Animator>();
        }

        if (animator != null && avatarOverride != null)
            animator.avatar = avatarOverride;

        if (animator != null && animator.avatar == null)
        {
            var avatar = FindAvatarInChildren();
            if (avatar != null)
                animator.avatar = avatar;
        }
#if UNITY_EDITOR
        TryAssignControllersInEditor();
        if (animator != null && animator.avatar == null)
            TryAssignAvatarFromModel();
#endif

        if (animator != null && animator.runtimeAnimatorController == null && idleController != null)
            animator.runtimeAnimatorController = idleController;
    }

    private void CacheParameters()
    {
        hasBoolParameter = false;
        if (animator == null || string.IsNullOrEmpty(boolParameter))
            return;

        foreach (var parameter in animator.parameters)
        {
            if (parameter != null && parameter.name == boolParameter && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasBoolParameter = true;
                break;
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!autoResolveAnimator)
            return;

        if (IsAnimatorInvalid(animator))
            animator = null;
    }
#endif

    private Animator FindBestAnimator()
    {
        var animators = GetComponentsInChildren<Animator>(true);
        Animator fallback = null;
        foreach (var candidate in animators)
        {
            if (!IsAnimatorInvalid(candidate))
            {
                if (candidate.avatar != null)
                    return candidate;
                if (fallback == null)
                    fallback = candidate;
            }
        }

        return fallback;
    }

    private bool IsAnimatorInvalid(Animator candidate)
    {
        if (candidate == null)
            return true;
        if (candidate.GetComponent<Canvas>() != null)
            return true;
        if (candidate.GetComponent<RectTransform>() != null)
            return true;
        return false;
    }

    private Avatar FindAvatarInChildren()
    {
        var animators = GetComponentsInChildren<Animator>(true);
        foreach (var candidate in animators)
        {
            if (candidate == null)
                continue;
            if (candidate.avatar != null)
                return candidate.avatar;
        }

        return null;
    }

#if UNITY_EDITOR
    private void TryAssignControllersInEditor()
    {
        if (idleController == null)
            idleController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultIdleControllerPath);
        if (talkController == null)
            talkController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultTalkControllerPath);
    }

    private void TryAssignAvatarFromModel()
    {
        if (animator == null || animator.avatar != null)
            return;

        var skinned = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skinned == null || skinned.sharedMesh == null)
            return;

        var path = AssetDatabase.GetAssetPath(skinned.sharedMesh);
        if (string.IsNullOrEmpty(path))
            return;

        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in assets)
        {
            if (asset is Avatar avatar)
            {
                animator.avatar = avatar;
                break;
            }
        }
    }
#endif
}
