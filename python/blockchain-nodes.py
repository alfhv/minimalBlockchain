import hashlib
import json
from time import time
from uuid import uuid4

import requests
from flask import Flask, jsonify, request
from urllib.parse import urlparse

class Blockchain:
	def __init__(self):
		self.chain = []
		self.current_transactions = []
		self.nodes = set()
		
		# Create the genesis block
		self.new_block(previous_hash=1, proof=100)
		
	def new_block(self, proof, previous_hash=None, proof_hash=None):
		"""
		Create a new Block in the Blockchain
		
		:param proof: <int> The proof given by the Proof of Work algorithm
		:param previous_hash: (Optional) <str> Hash of previous Block
		:return: <dict> New Block
		"""
		block = {
			'index': len(self.chain) + 1,
			'timestamp': time(),
			'transactions': self.current_transactions,
			'proof': proof,
			'proof_hash': proof_hash,
			'previous_hash': previous_hash or self.hash(self.chain[-1]),
		}

		# Reset the current list of transactions
		self.current_transactions = []

		self.chain.append(block)
		return block		
		
	def new_transaction(self, sender, recipient, amount):
		"""
		Creates a new transaction to go into the next mined Block

		:param sender: <str> Address of the Sender
		:param recipient: <str> Address of the Recipient
		:param amount: <int> Amount
		:return: <int> The index of the Block that will hold this transaction
		"""

		self.current_transactions.append({
			'sender': sender,
			'recipient': recipient,
			'amount': amount,
		})

		return self.last_block['index'] + 1
		
	@property
	def last_block(self):
		return self.chain[-1]
		
	@staticmethod
	def hash(block):
		"""
		Creates a SHA-256 hash of a Block
		:param block: <dict> Block
		:return: <str>
		"""
	
		s = sorted(block['transactions'], key=lambda t: (t['sender'], t['recipient'], t['amount']))
		result = ""
		for t in s:
			sender = t['sender']
			recipient = t['recipient']
			amount = t['amount']
			result = f'{result}{sender}{recipient}{amount}'
		
		b_i = block['index']
		b_ph = block['previous_hash']
		b_p = block['proof']
		b_p_h = block['proof_hash']
		b_t = block['timestamp']
		b_t_str = f'{b_t}'
		b_t_str = b_t_str.replace(".", "")
		to_hash = f'{b_i}{b_ph}{b_p}{b_p_h}{b_t_str}{result}'
		to_hash = to_hash.replace("None","")
		
		return hashlib.sha256(to_hash.encode()).hexdigest()
		
		"""
		# We must make sure that the Dictionary is Ordered, or we'll have inconsistent hashes
		block_string = json.dumps(block, sort_keys=True).encode()
		return hashlib.sha256(block_string).hexdigest()
		"""
		
	def proof_of_work(self, last_block):
		"""
		Simple Proof of Work Algorithm:
		- Find a number p' such that hash(pp') contains leading 4 zeroes, where p is the previous p'
		- p is the previous proof, and p' is the new proof
		:param last_block: <dict> last Block
		:return: <int>
		"""

		last_proof = last_block['proof']
		last_hash = self.hash(last_block)		
		proof = 0
		guess_hash = self.valid_proof(last_proof, proof, last_hash)
		
		while guess_hash[:4] != "0000":
			#self.valid_proof(last_proof, proof) is False:
			proof += 1
			guess_hash = self.valid_proof(last_proof, proof, last_hash)

		proof_result = {
			'proof': proof,
			'hash': guess_hash
		}	
			
		return proof_result
		
	def register_node(self, address):
		"""
		Add a new node to the list of nodes
		:param address: <str> Address of node. Eg. 'http://192.168.0.5:5000'
		:return: None
		"""

		parsed_url = urlparse(address)
		self.nodes.add(parsed_url.netloc)		
		
	
	def valid_chain(self, chain):
		"""
		Determine if a given blockchain is valid
		:param chain: <list> A blockchain
		:return: <bool> True if valid, False if not
		"""

		last_block = chain[0]
		current_index = 1

		while current_index < len(chain):
			block = chain[current_index]
			print(f'{last_block}')
			print(f'{block}')
			print("\n-----------\n")
			# Check that the hash of the block is correct
			if block['previous_hash'] != self.hash(last_block):
				return False

			# Check that the Proof of Work is correct
			if not self.valid_proof(last_block['proof'], block['proof']):
				return False

			last_block = block
			current_index += 1

		return True

	def resolve_conflicts(self):
		"""
		This is our Consensus Algorithm, it resolves conflicts
		by replacing our chain with the longest one in the network.
		:return: <bool> True if our chain was replaced, False if not
		"""

		neighbours = self.nodes
		new_chain = None

		# We're only looking for chains longer than ours
		max_length = len(self.chain)

		# Grab and verify the chains from all the nodes in our network
		for node in neighbours:
			response = requests.get(f'http://{node}/chain')

			if response.status_code == 200:
				length = response.json()['length']
				chain = response.json()['chain']

				# Check if the length is longer and the chain is valid
				if length > max_length and self.valid_chain(chain):
					max_length = length
					new_chain = chain

		# Replace our chain if we discovered a new, valid chain longer than ours
		if new_chain:
			self.chain = new_chain
			return True

		return False		
		
	@staticmethod
	def valid_proof(last_proof, proof, last_hash):
		"""
		Validates the Proof: Does hash(last_proof, proof) contain 4 leading zeroes?
		:param last_proof: <int> Previous Proof
		:param proof: <int> Current Proof
		:param last_hash: <str> The hash of the Previous Block
		:return: <bool> True if correct, False if not.
		"""

		guess = f'{last_proof}{proof}{last_hash}'.encode()
		guess_hash = hashlib.sha256(guess).hexdigest()
		return guess_hash
		# return guess_hash[:4] == "0000"		
				
# Instantiate our Node
app = Flask(__name__)

# Generate a globally unique address for this node
node_identifier = str(uuid4()).replace('-', '')

# Instantiate the Blockchain
blockchain = Blockchain()	
	
@app.route('/chain/mine', methods=['GET'])
def mine():
	# We run the proof of work algorithm to get the next proof...
	last_block = blockchain.last_block
	proof_result = blockchain.proof_of_work(last_block)
	
	# We must receive a reward for finding the proof.
	# The sender is "0" to signify that this node has mined a new coin.
	blockchain.new_transaction(
		sender="0",
		recipient=node_identifier,
		amount=1,
	)
	
	# Forge the new Block by adding it to the chain
	previous_hash = blockchain.hash(last_block)
	block = blockchain.new_block(proof_result['proof'], previous_hash, proof_result['hash'])		
	
	response = {
		'message': "New Block Forged",
		'block': block
	}
	return jsonify(response), 200	
  
@app.route('/transactions/new', methods=['POST'])
def new_transaction():
	values = request.get_json()

	# Check that the required fields are in the POST'ed data
	required = ['sender', 'recipient', 'amount']
	if not all(k in values for k in required):
		return 'Missing values', 400
		

	# Create a new Transaction
	index = blockchain.new_transaction(values['sender'], values['recipient'], values['amount'])

	response = {'message': f'Transaction will be added to Block {index}'}
	return jsonify(response), 201
	

@app.route('/chain/chain', methods=['GET'])
def full_chain():
	response = {
		'chain': blockchain.chain,
		'length': len(blockchain.chain),
	}
	return jsonify(response), 200
	
@app.route('/nodes/register', methods=['POST'])
def register_nodes():
	values = request.get_json()

	nodes = values.get('nodes')
	if nodes is None:
		return "Error: Please supply a valid list of nodes", 400

	for node in nodes:
		blockchain.register_node(node)

	response = {
		'message': 'New nodes have been added',
		'total_nodes': list(blockchain.nodes),
	}
	return jsonify(response), 201
	
if __name__ == '__main__':
	from argparse import ArgumentParser

	parser = ArgumentParser()
	parser.add_argument('-p', '--port', default=5000, type=int, help='port to listen on')
	args = parser.parse_args()
	port = args.port
	
	print("loading...")
	
	transactions = []
	"""
	transactions.append({
			'sender': "abc",
			'recipient': "def",
			'amount': 5,
		})	
	transactions.append({
			'sender': "abc",
			'recipient': "ddf",
			'amount': 5,
		})			
	
	
	s=sorted(transactions, key=lambda t: (t['sender'], t['recipient'], t['amount']))
	result = ""
	for t in s:
		sender = t['sender']
		recipient = t['recipient']
		amount = t['amount']
		result = f'{result}{sender}{recipient}{amount}'
		
	print(f'result: {result}')
	b_p_h = None
	b_t = 1534168821.5484922
	b_t_str = f'{b_t}'
	b_t_str = b_t_str.replace(".", "")
	
	to_hash = f'{1}{1}{100}{b_p_h}{b_t_str}{result}'	
	to_hash = to_hash.replace("None","")	
	
	print(f'to_hash: {to_hash}')
		
	h = hashlib.sha256(to_hash.encode()).hexdigest()
	print(f'h: {h}')
	
	#print(s)
	"""	
	app.run(host='0.0.0.0', port=port)
