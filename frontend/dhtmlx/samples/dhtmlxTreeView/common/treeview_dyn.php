<?php
	require_once("./config.php");
	/*
	
	top-level output
	[ {item}, {item}, {item} ]
	
	sub-level output
	[{id: $_REQUEST["id"], items: [
		{item}, {item}, {item}
	]}]
	
	*/
	
	header("Content-Type: text/plain");
	
	$db = new PDO("mysql:host={$mysql_host};dbname={$mysql_db}", $mysql_user, $mysql_pasw);

	if (isset($_GET["id"]))
		$id = $_GET["id"];
	else 
		$id = 0;

	$nodes = array();
	$r = $db->query("SELECT ".
				"t.id AS id, ".
				"t.pId AS pId, ".
				"t.text AS text, ".
				"IF ((SELECT COUNT(*) FROM tree_def AS p WHERE p.pId=t.id)>0, 1, 0) AS kids ".
				"FROM tree_def AS t ".
				"WHERE t.pId=".$db->quote($id));
		
	while ($o = $r->fetchObject()){
		$item = array("id" => $o->id, "text" => $o->text);
		if ($o->kids > 0) 
			$item["kids"] = true;
		$nodes[] = $item;
	}
	
	//to make loading detectable in samples
	if ($id != 0) sleep(1);
	
	if ($id > 0)
		$nodes = array(array("id" => $id, "items" => $nodes));
	
	echo json_encode($nodes);
?>
