using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.AI
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class PromptAttribute : System.Attribute
    {
        public string Prompt { get; set; }
        public PromptAttribute(string prompt)
        {
            this.Prompt = prompt;
        }
    }


}
