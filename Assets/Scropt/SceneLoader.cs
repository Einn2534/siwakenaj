using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] string sceneName; // 読み込むシーン名（Build Settingsに追加しておく）

    // ボタンの OnClick() に登録
    public void LoadTarget()
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // いまのシーンをリスタートしたいとき用（必要ならボタンに）
    public void ReloadCurrent()
    {
        var current = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(current, LoadSceneMode.Single);
    }
}
