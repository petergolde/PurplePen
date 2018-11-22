using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace PurplePen
{
    class DescriptionLocalize
    {
        XmlDocument xmldoc;
        XmlNode root;
        SymbolDB symbolDB;

        public DescriptionLocalize(SymbolDB symbolDB)
        {
            this.symbolDB = symbolDB;
        }

        void LoadXmlDocument()
        {
            xmldoc = new XmlDocument();
            xmldoc.PreserveWhitespace = true;
            xmldoc.Load(symbolDB.FileName);
            root = xmldoc.DocumentElement;
        }

        void SaveXmlDocument()
        {
            xmldoc.Save(symbolDB.FileName);
            symbolDB.Reload();
        }

        // Add a new language
        public void AddLanguage(SymbolLanguage symbolLanguage, string langIdCopyTextsFrom)
        {
            LoadXmlDocument();

            XmlNode newNode = symbolLanguage.CreateXmlNode(xmldoc);

            bool replaced = false;

            // Replace existing language node, if language already exists.
            XmlNodeList languageNodes = root.SelectNodes("/symbols/language");
            foreach (XmlElement langNode in languageNodes) {
                if (langNode.GetAttribute("lang") == symbolLanguage.LangId) {
                    langNode.ParentNode.ReplaceChild(newNode, langNode);
                    replaced = true;
                    break;
                }
            }

            // Add new language node.
            if (!replaced) {
                root.InsertAfter(newNode, languageNodes.Item(languageNodes.Count - 1));
                root.InsertBefore(xmldoc.CreateTextNode("\r\n\t"), newNode);

                CopyAllNames(langIdCopyTextsFrom, symbolLanguage.LangId);     // Copy all the names from another language to the new one.
                CopyAllTexts(langIdCopyTextsFrom, symbolLanguage.LangId);     // Copy all the texts from another language to the new one.
            }

            SaveXmlDocument();
        }

        // Add/change description names.
        public void CustomizeDescriptionNames(Dictionary<string, List<SymbolText>> symbolNames)
        {
            LoadXmlDocument();

            // Remove all names for a given language first.
            foreach (string symbolId in symbolNames.Keys) {
                foreach (SymbolText symbolText in symbolNames[symbolId]) {
                    RemoveSymbolName(symbolId, symbolText.Lang);
                }
            }

            foreach (string symbolId in symbolNames.Keys) {
                foreach (SymbolText symbolText in symbolNames[symbolId]) {
                    AddOneSymbolName(symbolId, symbolText);
                }
            }

            SaveXmlDocument();
        }

        // Remove all symbol name for a symbol/language combination
        private void RemoveSymbolName(string symbolId, string langId)
        {
            XmlNodeList textNodes = root.SelectNodes(string.Format("/symbols/symbol[@id='{0}']/name[@lang='{1}']", symbolId, langId));
            foreach (XmlNode textNode in textNodes)
                textNode.ParentNode.RemoveChild(textNode);
        }

        // Add one symbol name.
        private void AddOneSymbolName(string symbolId, SymbolText symbolText)
        {
           symbolText.Plural = false;
           symbolText.Gender = "";

            // Find existing symbol, for this symbolId. Could be more than one for ones that have different ones for different standards.
            XmlNodeList symbolNodes = root.SelectNodes(string.Format("/symbols/symbol[@id='{0}']", symbolId));

            foreach (XmlNode symbolNode in symbolNodes) {
                // The new node to insert/replace.
                XmlNode newNode = symbolText.CreateXmlElement(xmldoc, "name");

                // Add new name node
                symbolNode.PrependChild(newNode);
                symbolNode.InsertBefore(xmldoc.CreateTextNode("\r\n\t\t"), newNode);
            }
        }

        public void CustomizeDescriptionTexts(Dictionary<string, List<SymbolText>> descriptionText)
        {
            LoadXmlDocument();

            // Remove all description text for a given language first.
            foreach (string symbolId in descriptionText.Keys) {
                foreach (SymbolText symbolText in descriptionText[symbolId]) {
                    RemoveDescriptionText(symbolId, symbolText.Lang);
                }
            }

            foreach (string symbolId in descriptionText.Keys) {
                foreach (SymbolText symbolText in descriptionText[symbolId]) {
                    AddOneDescriptionText(symbolId, symbolText);
                }
            }

            SaveXmlDocument();
        }

        // Remove all description text for a symbol/language combination
        private void RemoveDescriptionText(string symbolId, string langId)
        {
            XmlNodeList textNodes = root.SelectNodes(string.Format("/symbols/symbol[@id='{0}']/text[@lang='{1}']", symbolId, langId));
            foreach (XmlNode textNode in textNodes)
                textNode.ParentNode.RemoveChild(textNode);
        }

        // Add one description text.
        private void AddOneDescriptionText(string symbolId, SymbolText symbolText)
        {
            // Find existing symbol, for this symbolId. Could be more than one for ones that have different ones for different standards.
            XmlNodeList symbolNodes = root.SelectNodes(string.Format("/symbols/symbol[@id='{0}']", symbolId));

            foreach (XmlNode symbolNode in symbolNodes) {
                // The new node to insert/replace.
                XmlNode newNode = symbolText.CreateXmlElement(xmldoc, "text");

                // Find existing name node, for this symbolId
                XmlNodeList nameNodeList = symbolNode.SelectNodes("name");
                XmlNode lastNameNode = nameNodeList[nameNodeList.Count - 1];

                // Add new text node
                XmlNode parent = lastNameNode.ParentNode;
                parent.InsertAfter(newNode, lastNameNode);
                parent.InsertBefore(xmldoc.CreateTextNode("\r\n\t\t"), newNode);
            }

        }

        // Copy all texts from one language to another.
        private void CopyAllTexts(string langIdFrom, string langIdTo)
        {
            XmlNodeList allTexts = root.SelectNodes(string.Format("/symbols/symbol/text[@lang='{0}']", langIdFrom));
            HashSet<SymbolText> handledSymbolTexts = new HashSet<SymbolText>();

            foreach (XmlNode node in allTexts) {
                SymbolText symText = new SymbolText();
                symText.ReadFromXmlElementNode((XmlElement) node);
                symText.Lang = langIdTo;
                XmlElement parentNode = (XmlElement)node.ParentNode;
                string symbolId = parentNode.GetAttribute("id");
                if (!handledSymbolTexts.Contains(symText)) {
                    AddOneDescriptionText(symbolId, symText);
                    handledSymbolTexts.Add(symText);
                }
            }
        }

        // Copy all names from one language to another.
        private void CopyAllNames(string langIdFrom, string langIdTo)
        {
            XmlNodeList allNames = root.SelectNodes(string.Format("/symbols/symbol/name[@lang='{0}']", langIdFrom));
            HashSet<string> handledSymbolsIds = new HashSet<string>();

            foreach (XmlNode node in allNames) {
                SymbolText symText = new SymbolText();
                symText.ReadFromXmlElementNode((XmlElement) node);
                symText.Lang = langIdTo;
                string symbolId = ((XmlElement)node.ParentNode).GetAttribute("id");
                if (!handledSymbolsIds.Contains(symbolId)) {
                    AddOneSymbolName(symbolId, symText);
                    handledSymbolsIds.Add(symbolId);
                }
            }
        }

        public void MergeSymbolsFile(string fileToMerge, string langId)
        {
            XmlDocument docToMerge;
            XmlNode rootToMerge;

            // Load documents.
            LoadXmlDocument();
            docToMerge = new XmlDocument();
            docToMerge.PreserveWhitespace = true;
            docToMerge.Load(fileToMerge);
            rootToMerge = docToMerge.DocumentElement;

            // Remove all language nodes from the current document.
            string xpath = string.Format("//*[@lang='{0}']", langId);
            foreach (XmlElement node in root.SelectNodes(xpath)) {
                node.ParentNode.RemoveChild(node);
            }

            // Copy nodes over
            foreach (XmlElement node in rootToMerge.SelectNodes(xpath)) {
                ImportAndInsertNode(node);
            }

            SaveXmlDocument();
        }

        // Import another node, and insert in the correct place.
        void ImportAndInsertNode(XmlElement oldNode)
        {
            XmlElement newNode = (XmlElement) xmldoc.ImportNode(oldNode, true);

            if (newNode.Name == "language") {
                XmlNodeList languageNodes = root.SelectNodes("/symbols/language");
                root.InsertAfter(newNode, languageNodes.Item(languageNodes.Count - 1));
                root.InsertBefore(xmldoc.CreateTextNode("\r\n\t"), newNode);
            }
            else if (newNode.Name == "name") {
                string id = ((XmlElement) oldNode.ParentNode).GetAttribute("id");
                XmlNodeList nameNodes = root.SelectNodes(string.Format("/symbols/symbol[@id='{0}']/name", id));
                XmlNode last = nameNodes.Item(nameNodes.Count - 1);
                last.ParentNode.InsertAfter(newNode, last);
                last.ParentNode.InsertBefore(xmldoc.CreateTextNode("\r\n\t\t"), newNode);
            }
            else if (newNode.Name == "text") {
                string id = ((XmlElement) oldNode.ParentNode).GetAttribute("id");
                XmlNodeList textNodes = root.SelectNodes(string.Format("/symbols/symbol[@id='{0}']/text", id));
                XmlNode last = textNodes.Item(textNodes.Count - 1);
                last.ParentNode.InsertAfter(newNode, last);
                last.ParentNode.InsertBefore(xmldoc.CreateTextNode("\r\n\t\t"), newNode);
            }
        }

    }
}
