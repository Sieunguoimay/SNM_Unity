using System.Collections.Generic;

public interface ISetupTearDown
{
    IEnumerable<ISetupTearDown> GetChildren();
    void Setup();
    void TearDown();
}