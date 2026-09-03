// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

library MerkleProofLibrary {

    function verify(bytes32 leaf, bytes32[] memory proof) internal pure returns (bool) {

        require(proof.length >= 2, "merkle proof, must have at least one sibling and of course a root");

        bytes32 currentHash = leaf;

        for (uint256 i = 0; i < proof.length - 1; i++) {

            bytes32 proofHash = proof[i];

            if (currentHash < proofHash) {
                currentHash = keccak256(abi.encodePacked(currentHash, proofHash));
            } else {
                currentHash = keccak256(abi.encodePacked(proofHash, currentHash));
            }
        }

        return currentHash == proof[proof.length - 1];
    }
}
