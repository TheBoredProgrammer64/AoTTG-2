﻿using Assets.Scripts.UI.Elements;
using Assets.Scripts.UI.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.InGame.Rebinds
{
    public class RebindsMenu : MonoBehaviour
    {
        public GameObject TabViewContent;
        public Button TabViewButton;
        public GameObject RebindsViewContent;
        public RebindElement RebindElementPrefab;
        public UiCheckbox UiCheckbox;

        private Type currentRebindType = typeof(InputHuman);
        
        private void Awake()
        {
            var inputEnums = new List<Type>
            {
                typeof(InputCannon),
                typeof(InputHuman),
                typeof(InputHorse),
                typeof(InputTitan),
                typeof(InputUi)
            };

            foreach (var inputEnum in inputEnums)
            {
                var button = Instantiate(TabViewButton);
                var text = inputEnum.Name.Replace("Input", string.Empty);
                button.name = $"{text}Button";
                button.GetComponentInChildren<Text>().text = text;
                button.onClick.AddListener(delegate { ShowRebinds(inputEnum); });
                button.transform.SetParent(TabViewContent.transform);
            }

            ShowRebinds(currentRebindType);
        }

        public void Default()
        {
            InputManager.SetDefaultRebinds(currentRebindType);
            ShowRebinds(currentRebindType);
        }

        public void Load()
        {
            InputManager.LoadRebinds(currentRebindType);
            ShowRebinds(currentRebindType);
        }

        public void Save()
        {
            if (currentRebindType == typeof(InputCannon))
            {
                SaveRebinds<InputCannon>();
            }
            else if (currentRebindType == typeof(InputHorse))
            {
                SaveRebinds<InputHorse>();
            }
            else if (currentRebindType == typeof(InputHuman))
            {
                SaveRebinds<InputHuman>();
                var gasBurstCheckbox = RebindsViewContent.GetComponentInChildren<UiCheckbox>();
                InputManager.GasBurstDoubleTap = gasBurstCheckbox.Value;
                InputManager.SaveOtherPlayerPrefs();
            }
            else if (currentRebindType == typeof(InputTitan))
            {
                SaveRebinds<InputTitan>();
            }
            else if (currentRebindType == typeof(InputUi))
            {
                SaveRebinds<InputUi>();
            }
        }

        private void ShowRebinds(Type inputEnum)
        {
            foreach (Transform child in RebindsViewContent.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            if (inputEnum == typeof(InputCannon))
            {
                CreateRebindElement<InputCannon>();
            }
            else if (inputEnum == typeof(InputHuman))
            {
                CreateRebindElement<InputHuman>();
                var gasBurstCheckbox = Instantiate(UiCheckbox);
                gasBurstCheckbox.transform.SetParent(RebindsViewContent.transform);
                gasBurstCheckbox.Value = InputManager.GasBurstDoubleTap;
                gasBurstCheckbox.Label = "Gas Burst double tap";
                gasBurstCheckbox.Initialize();
            }
            else if (inputEnum == typeof(InputHorse))
            {
                CreateRebindElement<InputHorse>();
            }
            else if (inputEnum == typeof(InputTitan))
            {
                CreateRebindElement<InputTitan>();
            }
            else if (inputEnum == typeof(InputUi))
            {
                CreateRebindElement<InputUi>();
            }

            currentRebindType = inputEnum;
        }

        private void CreateRebindElement<T>()
        {
            foreach (T input in Enum.GetValues(typeof(T)))
            {
                var key = InputManager.GetKey(input);
                var rebindElement = Instantiate(RebindElementPrefab);
                rebindElement.transform.SetParent(RebindsViewContent.transform);
                rebindElement.Label.text = input.ToString();
                rebindElement.SetInputKeycode(key);
            }
        }

        private void SaveRebinds<T>()
        {
            var rebindKeys = RebindsViewContent.GetComponentsInChildren<RebindElement>();
            var keys = rebindKeys.Select(x => x.Key).ToArray();
            InputManager.SaveRebinds<T>(keys);
        }
    }
}
