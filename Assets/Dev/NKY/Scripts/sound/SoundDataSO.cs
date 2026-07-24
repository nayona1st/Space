using UnityEngine;

namespace Dev.NKY.Scripts
{
    [CreateAssetMenu(fileName = "SoundData_", menuName = "So/SoundData", order = 0)]
    public class SoundDataSO : ScriptableObject
    {
        [Header("Audio Clips")]
        [Tooltip("여러 개 등록 시 무작위로 하나를 선택해 재생합니다 (타격음, 발소리 등에 효과적)")]
        [SerializeField] private AudioClip[] clips;

        [Header("Sound Settings")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float minPitch = 0.95f;
        [Range(0.1f, 3f)] public float maxPitch = 1.05f;
        public bool loop = false;

        /// <summary>
        /// 전달받은 AudioSource에 이 SO의 설정을 입히고 재생합니다.
        /// </summary>
        public void Play(AudioSource source)
        {
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning($"[SoundDataSO] {name}에 등록된 AudioClip이 없습니다!");
                return;
            }

            // 클립 무작위 선택 (사운드 반복에 따른 지루함 방지)
            AudioClip clip = clips[Random.Range(0, clips.Length)];

            source.clip = clip;
            source.volume = volume;
            source.pitch = Random.Range(minPitch, maxPitch); // 약간의 피치 변화
            source.loop = loop;
            source.Play();
        }
    }
}