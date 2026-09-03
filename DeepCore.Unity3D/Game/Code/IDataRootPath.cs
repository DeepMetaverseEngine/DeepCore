namespace Code;

public interface IClientData
{
    string DataRootPath { get; }

    string UIRoot { get; }
    string ResRoot { get; }
    string EditorRoot { get; }


    string ServerID { get; }
    string AccountID { get; }
    string AccountToken { get; }

    
}