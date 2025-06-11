using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Models
{
    public class DeviceConfiguration
    {

        public DeviceConfiguration()
        {
            InitializeDefaultParameters();
        }

        private void InitializeDefaultParameters()
        {
            // 基础参数配置
            // 采样率
            AddParameter("基础配置", "采样率", (int)ParameterType.SamplingRate, sizeof(byte), typeof(byte), (byte)0x00,
                typeof(EnumParameter), Parameter.GetSampleOptions());

            AddParameter("基础配置", "数据类型", (int)ParameterType.UploadDataType, sizeof(byte), typeof(byte), (byte)0x00,
                typeof(EnumParameter), new List<EnumOption>()
                {
                    new("速度", (byte)0x00),
                    new("位移", (byte)0x01),
                    new("加速度", (byte)0x02),
                    new("I、Q路信号", (byte)0x03),
                }); // 上传数据类型
            AddParameter("基础配置", "激光功率", (int)ParameterType.LaserPower, sizeof(byte), typeof(byte), (byte)0x64,
                typeof(IntegerNumberParameter), min: 0, max: 100); // 激光功率 (0-100%)

            // 信号处理参数
            AddParameter("信号处理", "低通滤波", (int)ParameterType.LowPassFilter, sizeof(byte), typeof(byte), (byte)0x00,
                typeof(EnumParameter), Parameter.GetLowPassOptions()); // 低通滤波
            AddParameter("信号处理", "高通滤波", (int)ParameterType.HighPassFilter, sizeof(byte), typeof(byte), (byte)0x00,
                typeof(EnumParameter),
                new List<EnumOption>()
                {
                    new("档位1", (byte)0x00),
                    new("档位2", (byte)0x01),
                    new("档位3", (byte)0x02),
                    new("档位4", (byte)0x03),
                    new("档位5", (byte)0x04),
                    new("档位6", (byte)0x05),
                    new("档位7", (byte)0x06),
                    new("档位8", (byte)0x07),
                    new("档位9", (byte)0x08),
                    new("档位10", (byte)0x09),
                }); // 高通滤波

            // 量程配置
            AddParameter("量程配置", "速度量程", (int)ParameterType.VelocityRange, sizeof(byte), typeof(byte), (byte)0x00,
                typeof(EnumParameter),
                new List<EnumOption>()
                {
                    new("档位1", (byte)0x00),
                    new("档位2", (byte)0x01),
                    new("档位3", (byte)0x02),
                    new("档位4", (byte)0x03),
                    new("档位5", (byte)0x04),
                    new("档位6", (byte)0x05),
                    new("档位7", (byte)0x06),
                    new("档位8", (byte)0x07),
                    new("档位9", (byte)0x08),
                    new("档位10", (byte)0x09),
                }); // 速度量程
            AddParameter("量程配置", "位移量程", (int)ParameterType.DisplacementRange, sizeof(byte), typeof(byte), (byte)0x00,
                typeof(EnumParameter),
                new List<EnumOption>()
                {
                    new("档位1", (byte)0x00),
                    new("档位2", (byte)0x01),
                    new("档位3", (byte)0x02),
                    new("档位4", (byte)0x03),
                    new("档位5", (byte)0x04),
                    new("档位6", (byte)0x05),
                    new("档位7", (byte)0x06),
                    new("档位8", (byte)0x07),
                    new("档位9", (byte)0x08),
                    new("档位10", (byte)0x09),
                }); // 位移量程
            AddParameter("量程配置", "加速度量程", (int)ParameterType.AccelerationRange, sizeof(byte), typeof(byte), (byte)0x00,
                typeof(EnumParameter),
                new List<EnumOption>()
                {
                    new("档位1", (byte)0x00),
                    new("档位2", (byte)0x01),
                    new("档位3", (byte)0x02),
                    new("档位4", (byte)0x03),
                    new("档位5", (byte)0x04),
                    new("档位6", (byte)0x05),
                    new("档位7", (byte)0x06),
                    new("档位8", (byte)0x07),
                    new("档位9", (byte)0x08),
                    new("档位10", (byte)0x09),
                }); // 加速度量程


            AddParameter("输出接口1配置", "输出类型", (int)ParameterType.AnalogOutputType1, sizeof(byte), typeof(byte),
                (byte)0x00,
                typeof(EnumParameter), new List<EnumOption>()
                {
                    new("速度", (byte)0x00),
                    new("位移", (byte)0x01),
                    new("加速度", (byte)0x02)
                });

            AddParameter("输出接口1配置", "模拟输出", (int)ParameterType.AnalogOutputSwitch1, sizeof(byte), typeof(byte),
                (byte)0x00,
                typeof(BoolParameter));

            AddParameter("输出接口2配置", "输出类型", (int)ParameterType.AnalogOutputType2, sizeof(byte), typeof(byte),
                (byte)0x00,
                typeof(EnumParameter), new List<EnumOption>()
                {
                    new("速度", (byte)0x00),
                    new("位移", (byte)0x01),
                    new("加速度", (byte)0x02)
                });

            AddParameter("输出接口2配置", "模拟输出", (int)ParameterType.AnalogOutputSwitch2, sizeof(byte), typeof(byte),
                (byte)0x00,
                typeof(BoolParameter));
            //AddParameter("算法参数", "参数111", 0x10000000, sizeof(float), typeof(float), (float)0x00000001, typeof(FloatNumberParameter),
            //    min: 0, max: 99999);
            //AddParameter("算法参数", "参数222", 0x10000001, sizeof(float), typeof(float), (float)0x00000001, typeof(FloatNumberParameter),
            //    min: 0, max: 99999);

            AddParameter("算法参数", "前端滤波", (int)ParameterType.FrontendFilter, sizeof(byte), typeof(byte), (byte)0x00,
                typeof(EnumParameter),
                new List<EnumOption>()
                {
                    new("100k", (byte)0x00),
                    new("300k", (byte)0x01),
                    new("500k", (byte)0x02),
                    new("1M", (byte)0x03),
                    new("5M", (byte)0x04),
                    new("8M", (byte)0x05),
                });
            AddParameter("算法参数", "前端滤波类型", (int)ParameterType.FrontendFilterType, sizeof(byte), typeof(byte), (byte)0x00,
                typeof(EnumParameter),
                new List<EnumOption>()
                {
                    new("hamming", (byte)0x00),
                    new("hann", (byte)0x01),
                    new("kaiser", (byte)0x02),
                });
            AddParameter("算法参数", "前端滤波开关", (int)ParameterType.FrontendFilterSwitch, sizeof(byte), typeof(byte),
                (byte)0x00,
                typeof(BoolParameter));

            AddParameter("算法参数", "去直流开关", (int)ParameterType.FrontendDcRemovalSwitch, sizeof(byte), typeof(byte),
                (byte)0x00,
                typeof(BoolParameter));
            AddParameter("算法参数", "正交修正开关", (int)ParameterType.FrontendOrthogonalityCorrectionSwitch, sizeof(byte), typeof(byte),
                (byte)0x00,
                typeof(BoolParameter));
            AddParameter("算法参数", "数据分段长度", (int)ParameterType.DataSegmentLength, sizeof(Int32), typeof(Int32),
                (Int32)1024,
                typeof(IntegerNumberParameter), min: 0, max: Int32.MaxValue);

            AddParameter("算法参数", "速度滤波开关", (int)ParameterType.VelocityLowPassFilterSwitch, sizeof(byte), typeof(byte),
                (byte)0x00,
                typeof(BoolParameter));
            AddParameter("算法参数", "位移滤波开关", (int)ParameterType.DisplacementLowPassFilterSwitch, sizeof(byte), typeof(byte),
                (byte)0x00,
                typeof(BoolParameter));
            AddParameter("算法参数", "加速度滤波开关", (int)ParameterType.AccelerationLowPassFilterSwitch, sizeof(byte), typeof(byte),
                (byte)0x00,
                typeof(BoolParameter));

            AddParameter("算法参数", "速度振幅修正", (int)ParameterType.VelocityAmpCorrection, sizeof(float), typeof(float),
                (float)1,
                typeof(FloatNumberParameter), min: 0, max: int.MaxValue);
            AddParameter("算法参数", "位移振幅修正", (int)ParameterType.DisplacementAmpCorrection, sizeof(float), typeof(float),
                (float)1,
                typeof(FloatNumberParameter), min: 0, max: int.MaxValue);
            AddParameter("算法参数", "加速度振幅修正", (int)ParameterType.AccelerationAmpCorrection, sizeof(float), typeof(float),
                (float)1,
                typeof(FloatNumberParameter), min: 0, max: int.MaxValue);

            //// 增加触发采样相关参数
            //AddParameter("触发采样", "触发采样使能", (int)ParameterType.TriggerSampleEnable, sizeof(byte), typeof(byte), (byte)0x00, typeof(BoolParameter));
            //AddParameter("触发采样", "触发采样模式", (int)ParameterType.TriggerSampleMode, sizeof(byte), typeof(byte), (byte)0x00, typeof(EnumParameter), new List<EnumOption>
            //{
            //    new("边沿触发", (byte)0x00),
            //    new("电平触发", (byte)0x01)
            //});
            //AddParameter("触发采样", "触发电平", (int)ParameterType.TriggerSampleLevel, sizeof(float), typeof(float), 0.0f, typeof(FloatNumberParameter), min: -10000, max: 10000);
            //AddParameter("触发采样", "触发边沿", (int)ParameterType.TriggerSampleEdge, sizeof(byte), typeof(byte), (byte)0x00, typeof(EnumParameter), new List<EnumOption>
            //{
            //    new("上升沿", (byte)0x00),
            //    new("下降沿", (byte)0x01)
            //});
            //AddParameter("触发采样", "采样长度", (int)ParameterType.TriggerSampleLength, sizeof(int), typeof(int), 1024, typeof(IntegerNumberParameter), min: 1, max: 1000000);


        }

        private readonly List<ParameterCategory> _categories = new();

        // 带分类的添加方法
        public void AddParameter(string category,string name, int address, int dataLength, Type valueType, object defaultValue, Type setType,  List<EnumOption> options=null,int min= 0,int max= 100)
        {
            var paramCategory = _categories.FirstOrDefault(c => c.Name == category);
            // 确保分类存在
            if (paramCategory==null)
            {
                paramCategory = new ParameterCategory(category);
                _categories.Add(paramCategory);
            }

            // 创建参数实例
            var param = CreateTypedParameter(name,address,  dataLength, valueType, defaultValue,setType, options,min,max);
            param.Category = category;

            // 添加参数到分类
            paramCategory.Parameters.Add(param);
        }

        // 类型化参数创建
        private DeviceParameter CreateTypedParameter(string name, int address, int dataLength, Type valueType,
            object defaultValue, Type setType, List<EnumOption> options = null,int min =0,int max=100)
        {
            DeviceParameter deviceParameter = null;


            if (setType == typeof(NumberParameter))
            {
                deviceParameter = new NumberParameter
                {
                    Name = name,
                    Address = address,
                    DataLength = dataLength,
                    MinValue = min,
                    MaxValue = max
                };
            }
            else if (setType == typeof(IntegerNumberParameter))
            {
                deviceParameter = new IntegerNumberParameter
                {
                    Name = name,
                    Address = address,
                    DataLength = dataLength,
                    MinValue = min,
                    MaxValue = max
                };
            }
            else if (setType == typeof(FloatNumberParameter))
            {
                deviceParameter = new FloatNumberParameter
                {
                    Name = name,
                    Address = address,
                    DataLength = dataLength,
                    MinValue = min,
                    MaxValue = max
                };
            }
            else if (setType == typeof(BoolParameter))
            {
                deviceParameter = new BoolParameter
                {
                    Name = name,
                    Address = address,
                    DataLength = dataLength,
                };
            }
            else if (setType == typeof(EnumCheckParameter))
            {
                deviceParameter = new EnumCheckParameter
                {
                    Name = name,
                    Address = address,
                    DataLength = dataLength,
                    Options = options
                };
            }
            else if (setType == typeof(EnumRadioParameter))
            {
                deviceParameter = new EnumRadioParameter
                {
                    Name = name,
                    Address = address,
                    DataLength = dataLength,
                    Options = options
                };
            }
            else if (setType == typeof(EnumParameter))
            {
                deviceParameter = new EnumParameter
                {
                    Name = name,
                    Address = address,
                    DataLength = dataLength,
                    Options = options
                };
            }

            if (valueType == typeof(byte))
            {
                deviceParameter.ValueType = typeof(byte);
                deviceParameter.Value = Convert.ToByte(defaultValue);
                //((NumberParameter)deviceParameter).MinValue = byte.MinValue;
                //((NumberParameter)deviceParameter).MaxValue = byte.MaxValue;
            }
            else if (valueType == typeof(bool))
            {
                deviceParameter.ValueType = typeof(bool);
                deviceParameter.Value = Convert.ToBoolean(defaultValue);
            }
            else if (valueType == typeof(Int32))
            {
                deviceParameter.ValueType = typeof(Int32);
                deviceParameter.Value = Convert.ToInt32(defaultValue);
            }
            else if (valueType == typeof(float))
            {
                deviceParameter.ValueType = typeof(float);
                deviceParameter.Value = Convert.ToSingle(defaultValue);
            }

            return deviceParameter;

        }

        // 获取所有分类
        public List<ParameterCategory> Categories => _categories;
    }
}
