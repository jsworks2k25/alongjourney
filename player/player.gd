extends CharacterBody2D

func _physics_process(delta: float) -> void:
	var direction := Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	if direction != Vector2.ZERO:
		velocity = direction * 64.0
	else:
		velocity = Vector2.ZERO
	move_and_slide()
