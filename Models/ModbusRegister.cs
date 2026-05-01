using Prism.Mvvm;

namespace MyProgram.Models
{
    /// <summary>
    /// Modbus 寄存器数据模型
    /// </summary>
    public class ModbusRegister : BindableBase
    {
        private int _address;
        /// <summary>
        /// 寄存器地址
        /// </summary>
        public int Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private int _value;
        /// <summary>
        /// 寄存器值
        /// </summary>
        public int Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        private string _description;
        /// <summary>
        /// 寄存器描述（如：温度、压力）
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }
    }
}