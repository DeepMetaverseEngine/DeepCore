// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface HERC20IMEvent {

    event SetAccessControl(address accessControl);

    event SetSupport(bool supportTransfer, bool supportMint, bool supportBurn);

}
