// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

library DeputyCenterType {

    struct BatchTransactions {
        address target;
        bytes callData;
        uint256 value;
    }

    struct BarrierRecord {
        address signer;
        uint256 barrierNonce;
    }
}
