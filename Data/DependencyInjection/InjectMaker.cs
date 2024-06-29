using System.Collections.Generic;

namespace DependencyInjection
{
    public class InjectMaker
    {
        private readonly object _target;
        private readonly Dictionary<string, object> _dic = new();

        private InjectMaker(object target)
        {
            _target = target;
        }

        public static InjectMaker Make(object target)
        {
            return new InjectMaker(target);
        }

        public InjectMaker Add(string key, object value)
        {
            _dic.Add(key, value);
            return this;
        }

        public void Inject()
        {
            DependencyInjector.Inject(_target, _dic);
        }
    }
}