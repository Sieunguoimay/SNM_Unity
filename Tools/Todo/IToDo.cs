public interface IToDo
{
    string Text { get; }
    bool Done { get; }
    UnityEngine.Object Target { get; }
}
public interface IToDoEditable : IToDo
{
    void SetText(string text);
    void SetDone(bool done);
    void SetTarget(UnityEngine.Object target);
}