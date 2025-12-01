/*
SQLyog Community v13.3.1 (64 bit)
MySQL - 8.0.42 : Database - interplanetery_tabledb_local
*********************************************************************
*/

/*!40101 SET NAMES utf8 */;

/*!40101 SET SQL_MODE=''*/;

/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE DATABASE /*!32312 IF NOT EXISTS*/`interplanetery_tabledb_local` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `interplanetery_tabledb_local`;

/*Table structure for table `fleet_info` */

DROP TABLE IF EXISTS `fleet_info`;

CREATE TABLE `fleet_info` (
  `id` int NOT NULL,
  `name` varchar(255) DEFAULT NULL,
  `type` int NOT NULL,
  `max_health` int NOT NULL,
  `attack_power` int NOT NULL,
  `move_speed` float NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

/*Data for the table `fleet_info` */

insert  into `fleet_info`(`id`,`name`,`type`,`max_health`,`attack_power`,`move_speed`) values 
(0,'정찰기',1,3,1,1),
(1,'구축함',1,6,3,0.8),
(2,'순양함',1,12,3,0.6),
(3,'전투순양함',1,24,6,0.4);

/*Table structure for table `map_info` */

DROP TABLE IF EXISTS `map_info`;

CREATE TABLE `map_info` (
  `id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  `description` text,
  `player1_homeworld_id` int NOT NULL,
  `player2_homeworld_id` int NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

/*Data for the table `map_info` */

insert  into `map_info`(`id`,`name`,`description`,`player1_homeworld_id`,`player2_homeworld_id`) values 
(0,'TRAINNING SCHOOL','짧은 전투를 통해 게임의 기초를 배웁니다.',1,0),
(1,'RUERY SPACE','인류가 처음으로 진출한 행성계.\n비록 현재는 그 찬란함을 잃었지만\n여전히 많은 이들의 꿈과 희망이 서려있다.',8,7),
(2,'TWISTED LIBRA SECTOR','초기에는 가지런한 천칭과 같은 모습이었으나,\n인공 블랙홀 실험장으로 활용되면서 현재와 같은 모습을 갖췄다.\n인공 블랙홀 실험은 오래 전에 중단되었지만\n그 여파로 인해 행성 간 효율적인 이동이 불가능한 상태다.',18,19);

/*Table structure for table `map_planet_info` */

DROP TABLE IF EXISTS `map_planet_info`;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

/*Data for the table `map_planet_info` */

insert  into `map_planet_info`(`id`,`map_id`,`planet_id`,`position_x`,`position_y`) values 
(0,0,0,8,0),
(1,0,1,-8,0),
(2,0,2,4,2.7),
(3,0,3,4,-2.7),
(4,0,4,-4,-2.7),
(5,0,5,-4,2.7),
(6,0,6,0,0),
(7,1,7,6,2.5),
(8,1,8,-6,-2.5),
(9,1,9,0,0),
(10,1,10,1,3),
(11,1,11,-1,-3),
(12,1,12,7,0),
(13,1,13,-7,0),
(14,1,14,5,-2.5),
(15,1,15,-5,2.5),
(16,1,16,3,1),
(17,1,17,-3,-1),
(18,2,18,-2,-0.5),
(19,2,19,2,0.5),
(20,2,20,-2,2.5),
(21,2,21,4,3),
(22,2,22,5.25,0),
(23,2,23,0,0),
(24,2,24,8,2),
(25,2,25,2,-2.5),
(26,2,26,-8,-2),
(27,2,27,-5.25,0),
(28,2,28,-4,-3);

/*Table structure for table `map_route_info` */

DROP TABLE IF EXISTS `map_route_info`;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

/*Data for the table `map_route_info` */

insert  into `map_route_info`(`id`,`map_id`,`planet_from_id`,`planet_to_id`) values 
(0,0,0,3),
(1,0,1,5),
(2,0,2,3),
(3,0,2,6),
(4,0,3,0),
(5,0,3,2),
(6,0,3,6),
(7,0,4,5),
(8,0,4,6),
(9,0,5,1),
(10,0,5,4),
(11,0,5,6),
(12,0,6,2),
(13,0,6,3),
(14,0,6,4),
(15,0,6,5),
(16,1,7,10),
(17,1,7,12),
(18,1,8,11),
(19,1,8,13),
(20,1,9,10),
(21,1,9,11),
(22,1,9,16),
(23,1,9,17),
(24,1,10,7),
(25,1,10,9),
(26,1,10,16),
(27,1,11,8),
(28,1,11,9),
(29,1,11,17),
(30,1,12,7),
(31,1,12,14),
(32,1,12,16),
(33,1,13,8),
(34,1,13,15),
(35,1,13,17),
(36,1,14,12),
(37,1,14,16),
(38,1,15,13),
(39,1,15,17),
(40,1,16,9),
(41,1,16,10),
(42,1,16,12),
(43,1,16,14),
(44,1,17,9),
(45,1,17,11),
(46,1,17,13),
(47,1,17,15),
(48,2,18,27),
(49,2,18,28),
(50,2,19,21),
(51,2,19,22),
(52,2,20,23),
(53,2,20,27),
(54,2,21,19),
(55,2,21,24),
(56,2,22,19),
(57,2,22,24),
(58,2,22,25),
(59,2,23,20),
(60,2,23,25),
(61,2,24,21),
(62,2,24,22),
(63,2,25,22),
(64,2,25,23),
(65,2,26,27),
(66,2,26,28),
(67,2,27,18),
(68,2,27,20),
(69,2,27,26),
(70,2,28,18),
(71,2,28,26);

/*Table structure for table `planet_info` */

DROP TABLE IF EXISTS `planet_info`;

CREATE TABLE `planet_info` (
  `id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  `gas` int NOT NULL,
  `mineral` int NOT NULL,
  `supply` int NOT NULL,
  `resource` varchar(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

/*Data for the table `planet_info` */

insert  into `planet_info`(`id`,`name`,`gas`,`mineral`,`supply`,`resource`) values 
(0,'Zeus',0,2,4,'Orange-Planet'),
(1,'Earth',0,2,4,'earth-like'),
(2,'Zephyros',0,1,2,'Storm-Planet'),
(3,'Draconis',0,1,2,'Lava-PLanet'),
(4,'Terrarosa',0,1,2,'Sand-Planet'),
(5,'Asus',0,1,2,'Ice-Planet'),
(6,'Intelli',2,1,4,'Water-Planet-With-Small-Islands'),
(7,'Aiur',0,2,4,'Orange-Planet'),
(8,'Auriga',0,2,4,'Storm-Planet'),
(9,'Kanterbury',1,2,4,'Red-Planet-With-Ice'),
(10,'Runeterra',0,1,2,'cyan-planet'),
(11,'Dunwall',0,1,2,'Ice-Planet'),
(12,'Kevin',0,1,2,'Purple-Planet'),
(13,'Azeroth',0,1,2,'Sand-Planet'),
(14,'Korhal',0,1,2,'Dark-PLanet'),
(15,'Torterine',0,1,2,'blue-planet'),
(16,'Char',2,1,2,'red-planet-sputnik'),
(17,'Maru',2,1,2,'earth-like'),
(18,'Elysion',0,2,4,'cyan-planet'),
(19,'Charon',0,2,4,'Storm-Planet'),
(20,'Xentaris',0,1,2,'Sand-Planet'),
(21,'Titan',0,1,2,'earth-like'),
(22,'Novares',2,1,4,'Storm-Planet'),
(23,'Golder',1,2,4,'Orange-Planet'),
(24,'Persephon',0,1,2,'Lava-PLanet'),
(25,'Lumina',0,1,2,'red-planet-sputnik'),
(26,'Astra',0,1,2,'Red-Planet-With-Ice'),
(27,'Althea',2,1,4,'Purple-Planet'),
(28,'Seraphis',0,1,2,'Ice-Planet');

/*Table structure for table `production_info` */

DROP TABLE IF EXISTS `production_info`;

CREATE TABLE `production_info` (
  `id` int NOT NULL AUTO_INCREMENT,
  `target_id` int NOT NULL,
  `production_time` float NOT NULL,
  `mineral_cost` int NOT NULL,
  `gas_cost` int NOT NULL,
  `supply_cost` int NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

/*Data for the table `production_info` */

insert  into `production_info`(`id`,`target_id`,`production_time`,`mineral_cost`,`gas_cost`,`supply_cost`) values 
(1,1,4000,35,0,2),
(2,2,5000,75,15,3),
(3,3,7000,100,75,4),
(6,0,1000,15,0,1);

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;
