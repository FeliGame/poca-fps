# POCA FPS
基于Unity ML-Agents的GAIL模仿学习+POCA强化学习算法的第一人称射击游戏。

## 训练环境配置
如果用户想在该项目基础上训练自己的强化学习模型，则依据本节操作（只是游玩可跳过本节）。依次在终端执行下列命令安装相关依赖。
``` shell 
conda create -n py3.10 python=3.10.12  # 创建环境，python具体版本号检查ML-Agents依赖
conda activate py3.10  # 激活环境

conda install grpcio==1.48.2  # 仅Apple M系列芯片需要指定该版本，新版本在编译时存在错误
pip install torch==2.1.1 torchvision torchaudio  # 仅Apple M系列芯片需要指定torch版本，否则对MPS支持会出错
pip install mlagents
```
若想利用Apple M系列的GPU加速，还需先找到mlagents包位置（并不是所有模型启用MPS都会得到加速）：
``` shell
pip show mlagents
```
然后在ML-Agents包的torch_utils目录下找到torch.py文件，按如下修改以添加MPS支持：
``` python
import os

from distutils.version import LooseVersion
import pkg_resources
from mlagents.torch_utils import cpu_utils
from mlagents.trainers.settings import TorchSettings
from mlagents_envs.logging_util import get_logger


logger = get_logger(__name__)


def assert_torch_installed():
    # Check that torch version 1.6.0 or later has been installed. If not, refer
    # user to the PyTorch webpage for install instructions.
    torch_pkg = None
    try:
        torch_pkg = pkg_resources.get_distribution("torch")
    except pkg_resources.DistributionNotFound:
        pass
    assert torch_pkg is not None and LooseVersion(torch_pkg.version) >= LooseVersion(
        "1.6.0"
    ), (
        "A compatible version of PyTorch was not installed. Please visit the PyTorch homepage "
        + "(https://pytorch.org/get-started/locally/) and follow the instructions to install. "
        + "Version 1.6.0 and later are supported."
    )


assert_torch_installed()

# This should be the only place that we import torch directly.
# Everywhere else is caught by the banned-modules setting for flake8
import torch  # noqa I201


torch.set_num_threads(cpu_utils.get_num_threads_to_use())
os.environ["KMP_BLOCKTIME"] = "0"


_device = torch.device("cpu")


def set_torch_config(torch_settings: TorchSettings) -> None:
    global _device

    if torch_settings.device is None:
        # * Original version (commented out):
        # device_str = "cuda" if torch.cuda.is_available() else "cpu"

        # * New version with MPS support:
        if torch.backends.mps.is_available():
            device_str = "mps"
        elif torch.cuda.is_available():
            device_str = "cuda"
        else:
            device_str = "cpu"
    else:
        device_str = torch_settings.device

    _device = torch.device(device_str)

    # * Original version (commented out):
    # if _device.type == "cuda":
    #     torch.set_default_device(_device.type)
    #     torch.set_default_dtype(torch.cuda.FloatTensor)
    # else:
    #     torch.set_default_dtype(torch.float32)

    # * New version with MPS support:
    print(f"DEVICE TYPE: {_device.type}")
    if _device.type == "cuda":
        torch.set_default_device(_device.type)
        torch.set_default_dtype(torch.cuda.FloatTensor)
    elif _device.type == "mps":
        torch.set_default_device(_device.type)
        torch.set_default_dtype(torch.float32)
    else:
        torch.set_default_dtype(torch.float32)

    logger.debug(f"default Torch device: {_device}")


# Initialize to default settings
set_torch_config(TorchSettings(device=None))

nn = torch.nn


def default_device():
    return _device
```
执行`` mlagents-learn -h ``，若出现``DEVICE TYPE: mps``则表示MPS启用成功。

## 训练
本项目Assets/Configs下的FPSAgent.yaml文件（训练配置文件）控制了训练所需的参数，然后执行：
```shell 
mlagents-learn /path/to/FPSAgent.yaml --run-id FPSAgent --force  # 用--force表示覆盖--run-id对应模型重新训练，用--resume表示接着上一次--run-id对应模型继续训练
```
执行该命令后，将在当前目录下创建results/[run-id]目录，并启动5005端口监听Unity Editor是否启动。点击Unity Editor的Play按钮，即可开始训练。

可随时按下Ctrl+C中止训练，此时会在results/[run-id]目录下生成一个onnx模型文件。

一些实用命令：

### 检视训练记录
``` shell
tensorboard --logdir results
```

### 可视化ONNX模型
``` shell
pip install netron
netron /path/to/FPSBehavior.onnx
```

## 推理
* 使用项目自带的模型：直接点击Play按钮即可。

* 使用自行训练的模型：将results/[run-id]目录下的onnx模型文件拖拽到项目中智能体的Behavior Parameters组件的Model值中，再点击Play按钮即可在该模型下执行推理。
