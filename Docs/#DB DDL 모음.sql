#DB DDL 모음

CREATE TABLE `fleet_info` (
  `id` int NOT NULL,
  `name` varchar(255) DEFAULT NULL,
  `type` int NOT NULL,
  `max_health` int NOT NULL,
  `attack_power` int NOT NULL,
  `move_speed` float NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci

CREATE TABLE `map_info` (
  `id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  `description` text,
  `player1_homeworld_id` int NOT NULL,
  `player2_homeworld_id` int NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci

CREATE TABLE `map_planet_info` (
  `id` int NOT NULL,
  `map_id` int NOT NULL,
  `planet_id` int NOT NULL,
  `position_x` float NOT NULL,
  `position_y` float NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `unique_map_planet` (`map_id`,`planet_id`),
  KEY `planet_id` (`planet_id`),
  KEY `idx_map_id` (`map_id`),
  KEY `idx_position` (`map_id`,`position_x`,`position_y`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci

CREATE TABLE `map_route_info` (
  `id` int NOT NULL,
  `map_id` int NOT NULL,
  `planet_from_id` int NOT NULL,
  `planet_to_id` int NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `unique_route` (`map_id`,`planet_from_id`,`planet_to_id`),
  KEY `planet_from_id` (`planet_from_id`),
  KEY `planet_to_id` (`planet_to_id`),
  KEY `idx_map_routes` (`map_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci

CREATE TABLE `planet_info` (
  `id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  `gas` int NOT NULL,
  `mineral` int NOT NULL,
  `supply` int NOT NULL,
  `resource` varchar(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci

CREATE TABLE `production_info` (
  `id` int NOT NULL AUTO_INCREMENT,
  `target_id` int NOT NULL,
  `production_time` float NOT NULL,
  `mineral_cost` int NOT NULL,
  `gas_cost` int NOT NULL,
  `supply_cost` int NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
