using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using PakalEditor.Annotations;

namespace PakalEditor.mvvm_stuff
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected void OnPropertyChanged<T>(Expression<Func<T>> property)
        {
            var memberExpression = (MemberExpression) property.Body;
            OnPropertyChanged(memberExpression.Member.Name);
        }

    }
}