﻿using Integrations;
using Integrations.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Modules
{
    public class Messanging : MonoBehaviour, Module
    {
        uint openedMessage;
        Integrations.Messanging module;
        public void Reset() { module = null; openedMessage = 0; }
        public void OnEnable()
        {
            if (!Manager.isReady || !Manager.provider.TryGetModule(out module)) { gameObject.SetActive(false); return; }
            if (Manager.Data.ActiveChild.Messages?.Count > 0) Refresh();
            else Reload();
        }
        public void Reload() => StartCoroutine(module.GetMessages(() => Refresh()));
        public void ReloadMessage() => StartCoroutine(module.LoadExtraMessageData(openedMessage, () => OpenMsg(Manager.Data.ActiveChild.Messages.FirstOrDefault(m => m.id == openedMessage))));

        public void Refresh()
        {
            var content = transform.Find("Content");
            foreach (Transform go in content) go.gameObject.SetActive(false);

            var isntEmpty = Manager.Data.ActiveChild.Messages?.Count != 0;

            if (!isntEmpty) { content.Find("Empty").gameObject.SetActive(true); return; }

            var list = content.Find("List").GetComponent<ScrollRect>();
            list.gameObject.SetActive(isntEmpty);
            var lContent = list.content;
            for (int i = 1; i < lContent.childCount; i++) Destroy(lContent.GetChild(i).gameObject);

            foreach (var message in Manager.Data.ActiveChild.Messages)
            {
                var go = Instantiate(lContent.GetChild(0).gameObject, lContent).transform;
                go.GetComponent<Button>().onClick.AddListener(() => OpenMsg(message));
                go.gameObject.name = message.id.ToString();
                var indicator = go.Find("Indicator").GetComponent<Image>();
                if (!message.read) indicator.color = new Color32(135, 135, 135, 255);
                else if (message.type == Message.Type.sent) indicator.color = new Color32(85, 115, 175, 255);
                else indicator.color = new Color32(0, 0, 0, 0);
                go.Find("Subject").GetComponent<Text>().text = message.subject;
                go.Find("Correspondents").GetComponent<Text>().text = string.Join(" / ", message.correspondents);
                go.Find("Date").GetComponent<Text>().text = message.date.ToString("dd/MM/yyyy HH:mm");
                go.gameObject.SetActive(true);
            }
        }

        public void OpenMsg(Message message)
        {
            openedMessage = message.id;
            message.read = true;
            var content = transform.Find("Content");
            foreach (Transform go in content) go.gameObject.SetActive(false);

            if (message.content == null) StartCoroutine(module.LoadExtraMessageData(message.id, () => SetData()));
            else SetData();

            void SetData()
            {
                var detail = content.Find("Detail");

                var top = detail.Find("Top").Find("Infos");
                top.Find("Subject").GetComponent<Text>().text = message.subject;
                top.Find("Correspondents").GetComponent<Text>().text = string.Join(" / ", message.correspondents);
                top.Find("Date").GetComponent<Text>().text = message.date.ToString("dd/MM/yyyy HH:mm");

                var contentPanel = detail.Find("Content").GetComponent<ScrollRect>().content;
                contentPanel.Find("Message Content").GetComponent<TMPro.TMP_InputField>().text = message.content;
                var docs = contentPanel.Find("Docs");
                for (int i = 2; i < docs.childCount; i++) Destroy(docs.GetChild(i).gameObject);
                foreach (var doc in message.documents)
                {
                    var docGo = Instantiate(docs.GetChild(1).gameObject, docs).transform;
                    docGo.GetComponent<TMPro.TextMeshProUGUI>().text = System.IO.Path.GetFileNameWithoutExtension(doc.name);
                    docGo.GetComponent<Button>().onClick.AddListener(() => UnityThread.executeCoroutine(module.OpenMessageAttachment(doc)));
                    docGo.gameObject.SetActive(true);
                }
                docs.gameObject.SetActive(message.documents.Count > 0);

                detail.gameObject.SetActive(true);
            }
        }
    }
}
