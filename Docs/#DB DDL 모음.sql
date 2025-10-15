#DB DDL 모음

CREATE TABLE `map_planets` (
  `id` int NOT NULL AUTO_INCREMENT,
  `map_id` int NOT NULL,
  `planet_id` int NOT NULL,
  `position_x` float NOT NULL,
  `position_y` float NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `unique_map_planet` (`map_id`,`planet_id`),
  KEY `planet_id` (`planet_id`),
  KEY `idx_map_id` (`map_id`),
  KEY `idx_position` (`map_id`,`position_x`,`position_y`),
  CONSTRAINT `map_planets_ibfk_1` FOREIGN KEY (`map_id`) REFERENCES `maps` (`id`) ON DELETE CASCADE,
  CONSTRAINT `map_planets_ibfk_2` FOREIGN KEY (`planet_id`) REFERENCES `planet_info` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci

CREATE TABLE `maps` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `description` text,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `player1_homeworld_id` int NOT NULL,
  `player2_homeworld_id` int NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci

CREATE TABLE `planet_info` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `gas` int NOT NULL,
  `mineral` int NOT NULL,
  `supply` int NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci

CREATE TABLE `planet_routes` (
  `id` int NOT NULL AUTO_INCREMENT,
  `map_id` int NOT NULL,
  `planet_from_id` int NOT NULL,
  `planet_to_id` int NOT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `unique_route` (`map_id`,`planet_from_id`,`planet_to_id`),
  KEY `planet_from_id` (`planet_from_id`),
  KEY `planet_to_id` (`planet_to_id`),
  KEY `idx_map_routes` (`map_id`),
  CONSTRAINT `planet_routes_ibfk_1` FOREIGN KEY (`map_id`) REFERENCES `maps` (`id`) ON DELETE CASCADE,
  CONSTRAINT `planet_routes_ibfk_2` FOREIGN KEY (`planet_from_id`) REFERENCES `planet_info` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `planet_routes_ibfk_3` FOREIGN KEY (`planet_to_id`) REFERENCES `planet_info` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `planet_routes_chk_1` CHECK ((`planet_from_id` < `planet_to_id`))
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
