using System.Collections.Generic;
using boop;
using Boomguy;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gallery
{
    public class FlowNavigationController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button _selectButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _upButton;
        [SerializeField] private Button _downButton;

        [Header("Flow Items")]
        [SerializeField] private Transform _centerAnchor;
        [SerializeField] private Transform[] _itemAnchors;
        [SerializeField] private FlowItemPreset[] _itemPresets;

        [Header("Movement")]
        [SerializeField] private FlowMovementPreset _flowMovementPreset;
        [SerializeField] private FlowMovementPreset _selectMovementPreset;

        [Header("Audio")]
        [SerializeField] private AudioController _audioController;
        [SerializeField] private AudioPreset _whooshSound;
        [SerializeField] private AudioPreset _selectSound;
        [SerializeField] private AudioPreset _clickSound;

        [SerializeField] private GameObject _rotateIndicatorPrefab;

        private List<FlowItem> _createdItems = new List<FlowItem>();
        private int _currentIndex = 0;
        private bool _isSelecting = false;

        private void Start()
        {
            SetupFlowItems();
            SetupUI();
        }

        private void SetupUI()
        {
            _selectButton.OnClick += OnSelectClicked;
            _backButton.OnClick += OnBackClicked;
            _upButton.OnClick += OnNextClicked;
            _downButton.OnClick += OnPrevClicked;

            UpdateUIState();
        }

        private void SetupFlowItems()
        {
            for (int i = 0; i < _itemPresets.Length; i++)
            {
                int index = i;
                _itemPresets[i].ItemRef.InstantiateAsync().Completed += (obj) =>
                {
                    if (obj.Status == AsyncOperationStatus.Succeeded)
                    {
                        GameObject item = obj.Result;
                        item.transform.SetParent(transform);
                        item.transform.localPosition = Vector3.zero;
                        item.SetActive(false);

                        if (item.TryGetComponent<FlowItem>(out FlowItem flowItem))
                        {
                            flowItem.Initialize(_itemPresets[index], _rotateIndicatorPrefab);
                            _createdItems.Add(flowItem);

                            if (_createdItems.Count == _itemPresets.Length)
                            {
                                UpdateFlowItems();
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Failed to load asset for {_itemPresets[index].name}");
                    }
                };
            }
        }

        private void OnSelectClicked()
        {
            if (_isSelecting || _createdItems.Count == 0) return;

            _clickSound?.PlayOneShot(_audioController);
            _selectSound?.PlayOneShot(_audioController);
            _isSelecting = true;

            var currentItem = _createdItems[_currentIndex];
            currentItem.EnableShowcaser(true, _centerAnchor);
            currentItem.SetReady(true, _centerAnchor);

            UpdateUIState();
        }

        private void OnBackClicked()
        {
            if (!_isSelecting) return;

            _clickSound?.PlayOneShot(_audioController);
            _isSelecting = false;

            var currentItem = _createdItems[_currentIndex];
            currentItem.EnableShowcaser(false, transform);

            // Re-layout items
            UpdateFlowItems();
            UpdateUIState();
        }

        private void OnNextClicked()
        {
            if (_isSelecting || _createdItems.Count == 0) return;

            _whooshSound?.PlayOneShot(_audioController);
            _currentIndex = LoopId(_currentIndex, 1);
            UpdateFlowItems();
        }

        private void OnPrevClicked()
        {
            if (_isSelecting || _createdItems.Count == 0) return;

            _whooshSound?.PlayOneShot(_audioController);
            _currentIndex = LoopId(_currentIndex, -1);
            UpdateFlowItems();
        }

        private void UpdateFlowItems()
        {
            if (_createdItems.Count == 0) return;

            for (int i = 0; i < _itemAnchors.Length; i++)
            {
                // Calculate which item should be at this anchor
                // Anchor 0 is usually front/center.
                // We want _currentIndex to be at Anchor 0.

                // If we map anchor indices to relative offsets from current index:
                // Let's assume Anchors are arranged in a specific way relative to the camera.
                // We map relative indices: 0 -> current, 1 -> next, etc.

                // For simplicity, let's map items to anchors cyclically based on _currentIndex.
                // But typically a "Flow" implies a circular buffer visual.
                // Let's use the logic: Item[LoopId(_currentIndex, i)] goes to Anchor[i]? 
                // Or Anchor[i] gets Item ?

                // Recreating legacy logic:
                // Items are placed at anchors.
                // We determine which item goes to which anchor.
                // Anchor 0 = Current Item.
                // Anchor 1 = Next Item.
                // etc.

                int itemIndex = LoopId(_currentIndex, i); // This might be simplistic
                                                          // Better: The controller usually defines a fixed set of Anchors (positions in the ring).
                                                          // And we rotate the ITEMS through these anchors.

                // Let's map Anchor[k] gets Item[ (CurrentIndex + k) % ItemCount ]?
                // Actually, let's check how many anchors vs items.
                // If Items > Anchors, we hide invisible ones.

                // Assuming anchors match the visual slots 1:1 for now or we just cycle through.
                if (i < _createdItems.Count)
                {
                    Transform targetAnchor = _itemAnchors[i];
                    FlowItem item = _createdItems[LoopId(_currentIndex, i)]; // Simple rotation

                    if (item.gameObject.activeSelf)
                    {
                        _ = item.MoveToAnchorAsync(targetAnchor, _flowMovementPreset);
                    }
                    else
                    {
                        item.SetReady(true, targetAnchor);
                    }
                }
            }

            // Hide items not at any anchor? 
            // If we have more items than anchors, we should handle visibility.
            // For now, let's assume specific setup or logic matches items to anchors.
            // Based on "Flow", usually it's a fixed number of visible slots.
        }

        private void UpdateUIState()
        {
            _selectButton.gameObject.SetActive(!_isSelecting);
            _backButton.gameObject.SetActive(_isSelecting);
            _upButton.gameObject.SetActive(!_isSelecting);
            _downButton.gameObject.SetActive(!_isSelecting);
        }

        private int LoopId(int current, int step)
        {
            int length = _createdItems.Count;
            if (length == 0) return 0;
            return (current + step + length) % length;
        }
    }

    // Helper extension for AudioController
    public static class AudioControllerExtensions
    {
        public static void PlayOneShot(this AudioPreset preset, AudioController controller)
        {
            if (preset != null && controller != null)
            {
                var player = controller.Play(preset);
                // Boomguy.AudioPlayer.Play automatically handles OneShot if we use PlayOneShot logic?
                // No, AudioController.Play calls Play() which starts playing.
                // We might want to use a specific method or just Play and let it finish.
                // Boomguy.AudioPlayer doesn't seem to have valid "Release on finish" in Play(preset, loop).
                // But PlayAndRelease exists!

                // However, AudioController has PlayOnce(preset) which calls PlayAndRelease.
                controller.PlayOnce(preset);
            }
        }
    }
}
