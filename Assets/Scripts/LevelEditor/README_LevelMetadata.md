# Level Metadata设计文档

目的：为关卡设计师提供人工标注关卡数据的方式
# 数据
## tag数据
可以给关卡加上tag（string）
数据类型：category
tag需要一个额外的数据文件进行存储，用于管理全部已有的tag
## 评分数据
作为一种特殊的，常驻的可以给关卡进行难度评分（1-5分）
数据类型：ordinal
## 通关数据
此关卡是否能够通关？
这个需要和validate以及puzzle solver配合。必须要能够在validate模式下通关，或者是被puzzle solver解出来，才能被mark为能够通关
数据类型：bool
## Comment
string，额外的手写评价

# UI
默认全部使用TMP
## button open metadata panel
用于开/关metadata panel
->
## panel metadata
## UI display tags
显示现有的全部tag
## UI add tag
设计成文本框，但是如果点击（选中输入），就自动开启一个dropdown，里面是之前已经有过的tag类型，点击dropdown里面的会自动填充tag。输入文字可以缩小匹配范围。如果没有匹配的，就视为新建tag
## UI rate star
做成5颗星的样式，或者是一个套用了星星mask的可点击的progress bar。但注意星级只能是整数或者0.5（比如4.5），不能是其他数。最低0.5星。0星是默认（未评价）的状态
## UI is solveable (read-only)
只读，不改数据
## UI comment
TMPtext+button+文本框，button用于修改/保存，text用于显示当前评论，文本框用于修改评论