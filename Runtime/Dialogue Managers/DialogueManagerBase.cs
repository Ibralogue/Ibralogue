                                $"No conversation called \"{choice.LeadingConversationName}\" found for choice \"{choice.ChoiceName}\" in \"{_currentConversation.Name}\".",
                                this);
                        onClickAction = () => StartConversation(ParsedConversations[conversationIndex]);
                        break;
                }

                choiceButtonInstance.GetComponentInChildren<TextMeshProUGUI>().text = choice.ChoiceName;
                choiceButtonInstance.OnChoiceClick.AddListener(onClickAction);

                _choiceButtonInstances.Add(choiceButtonInstance);
            }
        }

        /// <summary>
        /// Gets all methods for the current assembly, other specified assemblies, or all assemblies, and checks them against the
        /// DialogueFunction attribute.
        /// </summary>
        protected IEnumerable<MethodInfo> GetDialogueMethods()
        {
            List<Assembly> assemblies = new List<Assembly>();
            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (searchAllAssemblies) assemblies.AddRange(allAssemblies);
            else
                foreach (Assembly assembly in allAssemblies)
                {
                    string name = assembly.GetName().Name;
                    if (name == "Assembly-CSharp" || includedAssemblies.Contains(name) ||
                        assembly == Assembly.GetExecutingAssembly()) assemblies.Add(assembly);
                }

            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (Assembly assembly in assemblies)
            {
                IEnumerable<MethodInfo> allMethods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods())
                    .Where(m => m.GetCustomAttributes(typeof(DialogueFunctionAttribute), false).Length > 0);
                methods.AddRange(allMethods);
            }

            return methods;
        }

        /// <summary>
        /// Clears all text and Images in the dialogue box.
        /// </summary>
        protected virtual void ClearDialogueBox()
        {
            _linePlaying = false;
            nameText.text = string.Empty;
            sentenceText.text = string.Empty;

            foreach (ManagerPlugin plugin in managerPlugins)
            {
                plugin.Clear(_currentConversation, _lineIndex);
            }

            if (GetComponent<PortraitImagePlugin>() != null)
            {

            }

            if (_choiceButtonInstances == null)
                return;

            foreach (ChoiceButton choiceButton in _choiceButtonInstances)
            {
                choiceButton.OnChoiceClick.RemoveAllListeners();
                Destroy(choiceButton.gameObject);
            }

            _choiceButtonInstances.Clear();
        }
