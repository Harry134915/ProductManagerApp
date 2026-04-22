using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProductManagerApp.Infrastructure.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        //人话:“创建一个命令的时候，
        //你必须告诉我‘点了要干什么’，
        //但你可以选择要不要告诉我‘什么时候能点’。”

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        //这里的null表示:如果你没告诉我规则（null）
        //那我默认“随时都能点”
        //如果你告诉我规则
        //那我就按你给的规则判断
        public void Execute(object parameter) => _execute(parameter);
        //人话:“当有人让我执行命令时，
        //我就去调用之前你交给我的那个方法。”

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        //value 具体指的是：
        //WPF 在为每一个绑定了 Command 的 Button 创建的、
        //用来在 CommandManager.RequerySuggested 触发时
        //重新调用对应命令的 CanExecute() 并更新按钮状态的那个内部方法。
    }
}
