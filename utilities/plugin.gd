@tool
extends EditorPlugin

const AUTOLOADS: Dictionary = {
	"UIManager": "res://addons/GodotUtilities/src/ui_management/UIManager.cs",
	"Physics2D": "res://addons/GodotUtilities/src/logic/Physics2D.cs",
}

func _enter_tree():
	for name in AUTOLOADS:
		if not ProjectSettings.has_setting("autoload/" + name):
			add_autoload_singleton(name, AUTOLOADS[name])
	ProjectSettings.save()

func _exit_tree():
	for name in AUTOLOADS.keys():
		if ProjectSettings.has_setting("autoload/" + name):
			remove_autoload_singleton(name)
